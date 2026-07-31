using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.CQRS.CommunityQuestions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class CommunityQuestionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CommunityQuestionRepository _cqRepo;
    private readonly TagRepository _tagRepo;
    private readonly TestAuditLogService _auditLog;
    private readonly CommunityQuestionService _sut;

    public CommunityQuestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _cqRepo = new CommunityQuestionRepository(_db);
        _tagRepo = new TagRepository(_db);
        _auditLog = new TestAuditLogService();
        _sut = new CommunityQuestionService(_db, _cqRepo, _tagRepo, _auditLog);
    }

    public void Dispose() => _db.Dispose();

    #region Seed helpers

    private async Task<User> SeedUserAsync(
        long id = 99,
        string email = "author@test.com",
        string username = "author",
        string fullName = "Test Author",
        UserStatus status = UserStatus.Active)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Username = username,
            Password = "HashedPassword",
            FullName = fullName,
            Location = "Test",
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Tag> SeedTagAsync(
        long id = 1,
        string name = "C#",
        string slug = "c-sharp",
        EntityStatus status = EntityStatus.Active)
    {
        var tag = new Tag
        {
            Id = id,
            Name = name,
            Slug = slug,
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return tag;
    }

    private async Task<CommunityQuestion> SeedQuestionAsync(
        long id = 1,
        string title = "How does ASP.NET Core DI work?",
        string content = "Explain Dependency Injection in ASP.NET Core.",
        CommunityQuestionStatus status = CommunityQuestionStatus.Open,
        long authorId = 99,
        int answerCount = 0,
        bool isDeleted = false,
        DateTimeOffset? deletedAt = null,
        long? deletedBy = null,
        DateTimeOffset? closedAt = null)
    {
        var question = new CommunityQuestion
        {
            Id = id,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Content = content,
            AuthorId = authorId,
            Status = status,
            AnswerCount = answerCount,
            ViewCount = 0,
            VoteScore = 0,
            BookmarkCount = 0,
            EffDate = DateTimeOffset.UtcNow.AddDays(-10),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-5)
        };

        if (isDeleted)
        {
            question.DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
            question.DeletedBy = deletedBy;
        }

        if (closedAt != null)
            question.ClosedAt = closedAt;

        _db.CommunityQuestions.Add(question);
        await _db.SaveChangesAsync();
        return question;
    }

    private async Task<CommunityAnswer> SeedAnswerAsync(
        long id = 1,
        long questionId = 1,
        long authorId = 99,
        string content = "Dependency Injection is...",
        bool isAccepted = false)
    {
        var answer = new CommunityAnswer
        {
            Id = id,
            QuestionId = questionId,
            AuthorId = authorId,
            Content = content,
            IsAccepted = isAccepted,
            VoteScore = 0,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.CommunityAnswers.Add(answer);
        await _db.SaveChangesAsync();
        return answer;
    }

    private async Task SeedQuestionTagAsync(long questionId, long tagId)
    {
        _db.CommunityQuestionTags.Add(new CommunityQuestionTag
        {
            QuestionId = questionId,
            TagId = tagId,
            EffDate = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    #endregion

    #region GetQuestionsAsync

    [Fact]
    public async Task GetQuestions_ReturnsPaginatedList()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedQuestionAsync(id: 2);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetQuestions_SearchByTitle()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, title: "How does ASP.NET Core work?", content: "Explain ASP.NET Core fundamentals for beginners.");
        await SeedQuestionAsync(id: 2, title: "Entity Framework Core basics", content: "Introduction to Entity Framework and database access patterns.");

        var filter = new GetCommunityQuestionsRequest { Keyword = "ASP.NET Core", Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Contains("ASP.NET Core", result.Items[0].Title);
    }

    [Fact]
    public async Task GetQuestions_SearchByContent()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, content: "Explain Dependency Injection in detail with practical examples.");
        await SeedQuestionAsync(id: 2, content: "Entity Framework Core database migrations guide.");

        var filter = new GetCommunityQuestionsRequest { Keyword = "Dependency Injection", Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetQuestions_FilterByStatus()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);
        await SeedQuestionAsync(id: 2, status: CommunityQuestionStatus.Resolved);

        var filter = new GetCommunityQuestionsRequest { Status = "Resolved", Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("Resolved", result.Items[0].Status.Name);
    }

    [Fact]
    public async Task GetQuestions_FilterByAuthorId()
    {
        await SeedUserAsync(id: 10, username: "author1");
        await SeedUserAsync(id: 20, username: "author2");
        await SeedQuestionAsync(id: 1, authorId: 10);
        await SeedQuestionAsync(id: 2, authorId: 20);

        var filter = new GetCommunityQuestionsRequest { AuthorId = 10, Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(10, result.Items[0].Author.Id);
    }

    [Fact]
    public async Task GetQuestions_FilterByTagId()
    {
        await SeedUserAsync();
        await SeedTagAsync(id: 5, name: "aspnet-core");
        await SeedQuestionAsync(id: 1);
        await SeedQuestionAsync(id: 2);
        await SeedQuestionTagAsync(questionId: 1, tagId: 5);

        var filter = new GetCommunityQuestionsRequest { TagId = 5, Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Single(result.Items[0].Tags);
        Assert.Equal("aspnet-core", result.Items[0].Tags[0].Name);
    }

    [Fact]
    public async Task GetQuestions_FilterByHasAnswers()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, answerCount: 2);
        await SeedQuestionAsync(id: 2, answerCount: 0);

        var hasAnswersFilter = new GetCommunityQuestionsRequest { HasAnswers = true, Page = 1, PageSize = 10 };
        var noAnswersFilter = new GetCommunityQuestionsRequest { HasAnswers = false, Page = 1, PageSize = 10 };

        var hasAnswersResult = await _sut.GetQuestionsAsync(hasAnswersFilter);
        var noAnswersResult = await _sut.GetQuestionsAsync(noAnswersFilter);

        Assert.Single(hasAnswersResult.Items);
        Assert.Equal(2, hasAnswersResult.Items[0].AnswerCount);
        Assert.Single(noAnswersResult.Items);
        Assert.Equal(0, noAnswersResult.Items[0].AnswerCount);
    }

    [Fact]
    public async Task GetQuestions_SortByCreatedAtDesc()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, title: "First Question");
        await SeedQuestionAsync(id: 2, title: "Second Question");

        var filter = new GetCommunityQuestionsRequest
        {
            SortBy = "createdAt",
            SortDirection = "desc",
            Page = 1,
            PageSize = 10
        };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(1, result.Items[1].Id);
    }

    [Fact]
    public async Task GetQuestions_SortByTitleAsc()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, title: "Zebra Question");
        await SeedQuestionAsync(id: 2, title: "Apple Question");

        var filter = new GetCommunityQuestionsRequest
        {
            SortBy = "title",
            SortDirection = "asc",
            Page = 1,
            PageSize = 10
        };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal("Apple Question", result.Items[0].Title);
        Assert.Equal("Zebra Question", result.Items[1].Title);
    }

    [Fact]
    public async Task GetQuestions_ReturnsAnswerCount()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, answerCount: 5);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].AnswerCount);
    }

    [Fact]
    public async Task GetQuestions_ReturnsTags()
    {
        await SeedUserAsync();
        await SeedTagAsync(id: 1, name: "C#");
        await SeedTagAsync(id: 2, name: ".NET");
        await SeedQuestionAsync(id: 1);
        await SeedQuestionTagAsync(questionId: 1, tagId: 1);
        await SeedQuestionTagAsync(questionId: 1, tagId: 2);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Tags.Count);
    }

    [Fact]
    public async Task GetQuestions_ExcludesDeletedByDefault()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: false);
        await SeedQuestionAsync(id: 2, isDeleted: true);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetQuestions_IncludesDeletedWhenRequested()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: false);
        await SeedQuestionAsync(id: 2, isDeleted: true);

        var filter = new GetCommunityQuestionsRequest { IncludeDeleted = true, Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetQuestions_DefaultSortIsCreatedAtDesc()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedQuestionAsync(id: 2);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal(2, result.Items[0].Id);
    }

    [Fact]
    public async Task GetQuestions_PaginationRespectsPageSize()
    {
        await SeedUserAsync();
        for (int i = 1; i <= 5; i++)
            await SeedQuestionAsync(id: i);

        var filter = new GetCommunityQuestionsRequest { Page = 1, PageSize = 2 };

        var result = await _sut.GetQuestionsAsync(filter);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    #endregion

    #region GetQuestionByIdAsync

    [Fact]
    public async Task GetQuestionById_ReturnsDetail()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedAnswerAsync(id: 1, questionId: 1);

        var result = await _sut.GetQuestionByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("How does ASP.NET Core DI work?", result.Title);
        Assert.Single(result.Answers);
    }

    [Fact]
    public async Task GetQuestionById_ReturnsRelatedTags()
    {
        await SeedUserAsync();
        await SeedTagAsync(id: 1, name: "C#");
        await SeedQuestionAsync(id: 1);
        await SeedQuestionTagAsync(questionId: 1, tagId: 1);

        var result = await _sut.GetQuestionByIdAsync(1);

        Assert.Single(result.Tags);
        Assert.Equal("C#", result.Tags[0].Name);
    }

    [Fact]
    public async Task GetQuestionById_ThrowsNotFound_WhenQuestionDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.GetQuestionByIdAsync(9999));

        Assert.Equal(HttpStatusCodes.NotFound, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotFound, exception.ResponseCode);
    }

    [Fact]
    public async Task GetQuestionById_DoesNotExposeSensitiveUserInfo()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var result = await _sut.GetQuestionByIdAsync(1);

        Assert.NotNull(result.Author);
        Assert.Equal("Test Author", result.Author.DisplayName);
    }

    [Fact]
    public async Task GetQuestionById_ReturnsIsLocked_FromClosedAt()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow);

        var result = await _sut.GetQuestionByIdAsync(1);

        Assert.True(result.IsLocked);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task GetQuestionById_ReturnsIsUnlocked_WhenNotClosed()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var result = await _sut.GetQuestionByIdAsync(1);

        Assert.False(result.IsLocked);
        Assert.Null(result.ClosedAt);
    }

    #endregion

    #region LockQuestionAsync

    [Fact]
    public async Task LockQuestion_LocksUnlockedQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 1,
            LockedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-1"
        };

        var result = await _sut.LockQuestionAsync(command);

        Assert.True(result.IsLocked);
        Assert.NotNull(result.ClosedAt);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task LockQuestion_ThrowsConflict_WhenAlreadyLocked()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow);

        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 1,
            LockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyLocked, exception.ResponseCode);
    }

    [Fact]
    public async Task LockQuestion_ThrowsNotFound_WhenQuestionDoesNotExist()
    {
        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 9999,
            LockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.NotFound, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotFound, exception.ResponseCode);
    }

    [Fact]
    public async Task LockQuestion_ThrowsConflict_WhenDeleted()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: true);

        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 1,
            LockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyDeleted, exception.ResponseCode);
    }

    [Fact]
    public async Task LockQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 1,
            LockedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-lock-1"
        };

        await _sut.LockQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionLocked, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    [Fact]
    public async Task LockQuestion_Idempotent_WhenCalledTwiceOnLocked()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow);

        var command = new LockCommunityQuestionCommand
        {
            QuestionId = 1,
            LockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockQuestionAsync(command));

        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyLocked, exception.ResponseCode);
    }

    #endregion

    #region UnlockQuestionAsync

    [Fact]
    public async Task UnlockQuestion_UnlocksLockedQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow);

        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 1,
            UnlockedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-unlock-1"
        };

        var result = await _sut.UnlockQuestionAsync(command);

        Assert.False(result.IsLocked);
        Assert.Null(result.ClosedAt);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task UnlockQuestion_ThrowsConflict_WhenNotLocked()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 1,
            UnlockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotLocked, exception.ResponseCode);
    }

    [Fact]
    public async Task UnlockQuestion_ThrowsNotFound_WhenQuestionDoesNotExist()
    {
        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 9999,
            UnlockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.NotFound, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotFound, exception.ResponseCode);
    }

    [Fact]
    public async Task UnlockQuestion_ThrowsConflict_WhenDeleted()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: true);

        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 1,
            UnlockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyDeleted, exception.ResponseCode);
    }

    [Fact]
    public async Task UnlockQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow);

        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 1,
            UnlockedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-unlock-2"
        };

        await _sut.UnlockQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionUnlocked, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    [Fact]
    public async Task UnlockQuestion_Idempotent_WhenCalledTwiceOnUnlocked()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new UnlockCommunityQuestionCommand
        {
            QuestionId = 1,
            UnlockedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockQuestionAsync(command));

        Assert.Equal(ResponseCodes.CommunityQuestionNotLocked, exception.ResponseCode);
    }

    #endregion

    #region DeleteQuestionAsync

    [Fact]
    public async Task DeleteQuestion_SoftDeletesActiveQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedAnswerAsync(id: 1, questionId: 1);
        await SeedAnswerAsync(id: 2, questionId: 1);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Violation of terms",
            DeletedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-delete-1"
        };

        var result = await _sut.DeleteQuestionAsync(command);

        Assert.True(result.IsDeleted);
        Assert.NotNull(result.DeletedAt);
        Assert.Equal(1, result.Id);

        // Verify answers still exist
        var answers = await _db.CommunityAnswers.Where(a => a.QuestionId == 1).ToListAsync();
        Assert.Equal(2, answers.Count);

        // Verify question is soft-deleted
        var question = await _db.CommunityQuestions.FindAsync(1L);
        Assert.NotNull(question!.DeletedAt);
        Assert.Equal(99, question.DeletedBy);
    }

    [Fact]
    public async Task DeleteQuestion_ThrowsConflict_WhenAlreadyDeleted()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: true);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Violation of terms",
            DeletedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyDeleted, exception.ResponseCode);
    }

    [Fact]
    public async Task DeleteQuestion_ThrowsNotFound_WhenQuestionDoesNotExist()
    {
        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 9999,
            Reason = "Violation of terms",
            DeletedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.NotFound, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotFound, exception.ResponseCode);
    }

    [Fact]
    public async Task DeleteQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Violation of terms",
            DeletedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-delete-2"
        };

        await _sut.DeleteQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionDeleted, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    [Fact]
    public async Task DeleteQuestion_DoesNotPhysicallyDeleteAnswers()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedAnswerAsync(id: 1, questionId: 1);
        await SeedAnswerAsync(id: 2, questionId: 1);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Violation of terms",
            DeletedByUserId = 99
        };

        await _sut.DeleteQuestionAsync(command);

        var answerCount = await _db.CommunityAnswers.CountAsync(a => a.QuestionId == 1);
        Assert.Equal(2, answerCount);
    }

    [Fact]
    public async Task DeleteQuestion_DoesNotPhysicallyDeleteTagMappings()
    {
        await SeedUserAsync();
        await SeedTagAsync(id: 1);
        await SeedQuestionAsync(id: 1);
        await SeedQuestionTagAsync(questionId: 1, tagId: 1);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Violation of terms",
            DeletedByUserId = 99
        };

        await _sut.DeleteQuestionAsync(command);

        var tagMappingCount = await _db.CommunityQuestionTags.CountAsync(cqt => cqt.QuestionId == 1);
        Assert.Equal(1, tagMappingCount);
    }

    #endregion

    #region RestoreQuestionAsync

    [Fact]
    public async Task RestoreQuestion_RestoresDeletedQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: true);

        var command = new RestoreCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Restored by mistake",
            RestoredByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-restore-1"
        };

        var result = await _sut.RestoreQuestionAsync(command);

        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
        Assert.Equal(1, result.Id);

        // Verify in DB
        var question = await _db.CommunityQuestions.FindAsync(1L);
        Assert.NotNull(question);
        Assert.Null(question.DeletedAt);
        Assert.Null(question.DeletedBy);
    }

    [Fact]
    public async Task RestoreQuestion_ThrowsConflict_WhenNotDeleted()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: false);

        var command = new RestoreCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Restored by mistake",
            RestoredByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestoreQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotDeleted, exception.ResponseCode);
    }

    [Fact]
    public async Task RestoreQuestion_ThrowsNotFound_WhenQuestionDoesNotExist()
    {
        var command = new RestoreCommunityQuestionCommand
        {
            QuestionId = 9999,
            Reason = "Restored by mistake",
            RestoredByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestoreQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.NotFound, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionNotFound, exception.ResponseCode);
    }

    [Fact]
    public async Task RestoreQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, isDeleted: true);

        var command = new RestoreCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Restored by mistake",
            RestoredByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-restore-2"
        };

        await _sut.RestoreQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionRestored, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    #endregion

    #region ChangeStatusAsync

    [Fact]
    public async Task ChangeStatus_ToResolved_WithAcceptedAnswer_Succeeds()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);
        await SeedAnswerAsync(id: 1, questionId: 1, isAccepted: true);

        // Update the question's AcceptedAnswerId
        var question = await _db.CommunityQuestions.FindAsync(1L);
        question!.AcceptedAnswerId = 1;
        await _db.SaveChangesAsync();

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Resolved,
            Reason = "Best answer selected",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-status-1"
        };

        var result = await _sut.ChangeStatusAsync(command);

        Assert.Equal("Open", result.PreviousStatus);
        Assert.Equal("Resolved", result.CurrentStatus);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task ChangeStatus_ToResolved_WithoutAcceptedAnswer_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Resolved,
            Reason = "Mark as resolved",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionResolvedRequiresAcceptedAnswer, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_ToResolved_WithHiddenAcceptedAnswer_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        var answer = await SeedAnswerAsync(id: 1, questionId: 1, isAccepted: true);
        answer.IsHidden = true;
        await _db.SaveChangesAsync();

        var question = await _db.CommunityQuestions.FindAsync(1L);
        question!.AcceptedAnswerId = 1;
        await _db.SaveChangesAsync();

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Resolved,
            Reason = "Mark as resolved",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAcceptedAnswerHidden, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_ToAnswered_WithValidAnswers_Succeeds()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);
        await SeedAnswerAsync(id: 1, questionId: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Answered,
            Reason = "Has answers",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-status-2"
        };

        var result = await _sut.ChangeStatusAsync(command);

        Assert.Equal("Answered", result.CurrentStatus);
    }

    [Fact]
    public async Task ChangeStatus_ToAnswered_WithNoAnswers_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Answered,
            Reason = "Mark as answered",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAnsweredRequiresAnswers, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_ToClosed_SetsClosedAt()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Closed,
            Reason = "Duplicate question",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-status-3"
        };

        var result = await _sut.ChangeStatusAsync(command);

        Assert.Equal("Closed", result.CurrentStatus);
        Assert.NotNull(result.ClosedAt);
    }

    [Fact]
    public async Task ChangeStatus_ToOpen_FromClosed_ClearsClosedAt()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Closed, closedAt: DateTimeOffset.UtcNow);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Open,
            Reason = "Reopen",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-status-4"
        };

        var result = await _sut.ChangeStatusAsync(command);

        Assert.Equal("Open", result.CurrentStatus);
        Assert.Null(result.ClosedAt);
    }

    [Fact]
    public async Task ChangeStatus_SameStatus_ThrowsConflict()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Open,
            Reason = "Set to open again",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyInTargetStatus, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_InvalidEnumValue_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = 99,
            Reason = "Invalid status",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionInvalidStatus, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_WithoutReason_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Hidden,
            Reason = "",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeStatusAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionReasonRequired, exception.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new ChangeCommunityQuestionStatusCommand
        {
            QuestionId = 1,
            Status = (int)CommunityQuestionStatus.Hidden,
            Reason = "Spam content",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-status-audit"
        };

        await _sut.ChangeStatusAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionStatusChanged, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    #endregion

    #region HideQuestionAsync

    [Fact]
    public async Task HideQuestion_HidesVisibleQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);

        var command = new HideCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Spam content",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-hide-1"
        };

        var result = await _sut.HideQuestionAsync(command);

        Assert.Equal("Hidden", result.Status);
        Assert.NotNull(result.HiddenAt);
        Assert.Equal(99, result.HiddenBy);
    }

    [Fact]
    public async Task HideQuestion_ThrowsConflict_WhenAlreadyHidden()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Hidden);

        var command = new HideCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Already hidden",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.HideQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyHidden, exception.ResponseCode);
    }

    [Fact]
    public async Task HideQuestion_DoesNotSetDeletedAt()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new HideCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Spam",
            ModeratedByUserId = 99
        };

        await _sut.HideQuestionAsync(command);

        var question = await _db.CommunityQuestions.FindAsync(1L);
        Assert.Null(question!.DeletedAt);
    }

    [Fact]
    public async Task HideQuestion_WithoutReason_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new HideCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.HideQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionReasonRequired, exception.ResponseCode);
    }

    [Fact]
    public async Task HideQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new HideCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Spam content",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-hide-audit"
        };

        await _sut.HideQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionHidden, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    #endregion

    #region CloseQuestionAsync

    [Fact]
    public async Task CloseQuestion_ClosesOpenQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);

        var command = new CloseCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Duplicate question",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-close-1"
        };

        var result = await _sut.CloseQuestionAsync(command);

        Assert.Equal("Closed", result.Status);
        Assert.NotNull(result.ClosedAt);
        Assert.Equal(99, result.ClosedBy);
    }

    [Fact]
    public async Task CloseQuestion_ThrowsConflict_WhenAlreadyClosed()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow, status: CommunityQuestionStatus.Closed);

        var command = new CloseCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Already closed",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CloseQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyClosed, exception.ResponseCode);
    }

    [Fact]
    public async Task CloseQuestion_WithoutReason_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new CloseCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CloseQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionReasonRequired, exception.ResponseCode);
    }

    [Fact]
    public async Task CloseQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new CloseCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Duplicate question",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-close-audit"
        };

        await _sut.CloseQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionClosed, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    #endregion

    #region ReopenQuestionAsync

    [Fact]
    public async Task ReopenQuestion_ReopensClosedQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, closedAt: DateTimeOffset.UtcNow, status: CommunityQuestionStatus.Closed);

        var command = new ReopenCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Valid discussion",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-reopen-1"
        };

        var result = await _sut.ReopenQuestionAsync(command);

        Assert.Equal("Open", result.Status);
        Assert.Null(result.ClosedAt);
    }

    [Fact]
    public async Task ReopenQuestion_ReopensHiddenQuestion()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Hidden);

        var command = new ReopenCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Valid question",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-reopen-2"
        };

        var result = await _sut.ReopenQuestionAsync(command);

        Assert.Equal("Open", result.Status);
        Assert.Null(result.ClosedAt);
    }

    [Fact]
    public async Task ReopenQuestion_ThrowsConflict_WhenAlreadyOpen()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Open);

        var command = new ReopenCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Already open",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ReopenQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.Conflict, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionAlreadyOpen, exception.ResponseCode);
    }

    [Fact]
    public async Task ReopenQuestion_WithoutReason_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Closed);

        var command = new ReopenCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "",
            ModeratedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ReopenQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionReasonRequired, exception.ResponseCode);
    }

    [Fact]
    public async Task ReopenQuestion_WritesAuditLog()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, status: CommunityQuestionStatus.Hidden);

        var command = new ReopenCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "Valid discussion",
            ModeratedByUserId = 99,
            IpAddress = "127.0.0.1",
            UserAgent = "Test",
            TraceId = "trace-reopen-audit"
        };

        await _sut.ReopenQuestionAsync(command);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityQuestionReopened, _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
    }

    #endregion

    #region DeleteQuestionAsync — Reason Required

    [Fact]
    public async Task DeleteQuestion_WithoutReason_Throws()
    {
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);

        var command = new DeleteCommunityQuestionCommand
        {
            QuestionId = 1,
            Reason = "",
            DeletedByUserId = 99
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteQuestionAsync(command));

        Assert.Equal(HttpStatusCodes.BadRequest, exception.StatusCode);
        Assert.Equal(ResponseCodes.CommunityQuestionReasonRequired, exception.ResponseCode);
    }

    #endregion
}
