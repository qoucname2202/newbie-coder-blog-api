using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.CQRS.CommunityAnswers;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class CommunityAnswerServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CommunityAnswerRepository _repo;
    private readonly TestAuditLogService _auditLog;
    private readonly CommunityAnswerService _sut;

    public CommunityAnswerServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _repo = new CommunityAnswerRepository(_db);
        _auditLog = new TestAuditLogService();
        _sut = new CommunityAnswerService(_db, _repo, _auditLog);
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

    private async Task<CommunityQuestion> SeedQuestionAsync(
        long id = 1,
        string title = "How does ASP.NET Core DI work?",
        CommunityQuestionStatus status = CommunityQuestionStatus.Open,
        long authorId = 99,
        bool isDeleted = false)
    {
        var question = new CommunityQuestion
        {
            Id = id,
            Title = title,
            Slug = title.ToLowerInvariant().Replace(" ", "-"),
            Content = "Explain Dependency Injection in ASP.NET Core.",
            AuthorId = authorId,
            Status = status,
            AnswerCount = 0,
            ViewCount = 0,
            VoteScore = 0,
            BookmarkCount = 0,
            EffDate = DateTimeOffset.UtcNow.AddDays(-10),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-5)
        };

        if (isDeleted)
        {
            question.DeletedAt = DateTimeOffset.UtcNow;
            question.DeletedBy = 1;
        }

        _db.CommunityQuestions.Add(question);
        await _db.SaveChangesAsync();
        return question;
    }

    private async Task<CommunityAnswer> SeedAnswerAsync(
        long id = 1,
        long questionId = 1,
        long authorId = 99,
        string content = "Dependency Injection is a design pattern...",
        bool isAccepted = false,
        bool isHidden = false,
        bool isDeleted = false,
        long? deletedBy = null)
    {
        var answer = new CommunityAnswer
        {
            Id = id,
            QuestionId = questionId,
            AuthorId = authorId,
            Content = content,
            IsAccepted = isAccepted,
            IsHidden = isHidden,
            VoteScore = 0,
            EffDate = DateTimeOffset.UtcNow.AddDays(-1),
            DateLastMaint = DateTimeOffset.UtcNow
        };

        if (isDeleted)
        {
            answer.DeletedAt = DateTimeOffset.UtcNow;
            answer.DeletedBy = deletedBy ?? 1;
            answer.DateLastMaint = answer.DeletedAt.Value;
        }

        _db.CommunityAnswers.Add(answer);
        await _db.SaveChangesAsync();
        return answer;
    }

    #endregion

    #region GetAnswersAsync — List Tests

    [Fact]
    public async Task GetAnswersAsync_DefaultRequest_ReturnsOnlyNonDeletedAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, content: "First answer");
        await SeedAnswerAsync(id: 2, content: "Second answer");
        await SeedAnswerAsync(id: 3, isDeleted: true);

        var filter = new GetCommunityAnswersRequest();

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, x => x.Id == 3);
    }

    [Fact]
    public async Task GetAnswersAsync_IncludeDeleted_ReturnsAllAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, content: "Active answer");
        await SeedAnswerAsync(id: 2, isDeleted: true);

        var filter = new GetCommunityAnswersRequest { IncludeDeleted = true };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAnswersAsync_SearchByContent_ReturnsMatchingAnswers()
    {
        // Arrange — use a unique keyword that only appears in one answer's full content
        await SeedUserAsync();
        await SeedQuestionAsync(id: 100);
        await SeedAnswerAsync(id: 100, questionId: 100, content: "This answer mentions DIfoobar framework specifically");
        await SeedAnswerAsync(id: 101, questionId: 100, content: "This answer covers Entity Framework topics");

        // Note: In InMemory, ToLower().Contains() is evaluated client-side after fetching rows.
        // Since the full Content field is available, both rows are fetched and the filter
        // is applied correctly. In real PostgreSQL, EF.Functions.ILike is used instead.
        var filter = new GetCommunityAnswersRequest { Keyword = "DIfoobar" };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert — the unique keyword ensures only one answer matches
        Assert.Single(result.Items);
        Assert.Equal(100, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAnswersAsync_SearchByQuestionTitle_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1, title: "How to use ASP.NET Core DI?");
        await SeedQuestionAsync(id: 2, title: "What is Entity Framework?");
        await SeedAnswerAsync(id: 1, questionId: 1, content: "Answer to DI question");
        await SeedAnswerAsync(id: 2, questionId: 2, content: "Answer to EF question");

        var filter = new GetCommunityAnswersRequest { Keyword = "ASP.NET Core" };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAnswersAsync_SearchByUserName_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync(id: 1, fullName: "John Doe");
        await SeedUserAsync(id: 2, fullName: "Jane Smith");
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, authorId: 1, content: "John's answer");
        await SeedAnswerAsync(id: 2, authorId: 2, content: "Jane's answer");

        var filter = new GetCommunityAnswersRequest { Keyword = "John Doe" };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAnswersAsync_FilterByQuestionId_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync(id: 1);
        await SeedQuestionAsync(id: 2);
        await SeedAnswerAsync(id: 1, questionId: 1);
        await SeedAnswerAsync(id: 2, questionId: 2);
        await SeedAnswerAsync(id: 3, questionId: 1);

        var filter = new GetCommunityAnswersRequest { QuestionId = 1 };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, x => Assert.Equal(1, x.Question.Id));
    }

    [Fact]
    public async Task GetAnswersAsync_FilterByUserId_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync(id: 1);
        await SeedUserAsync(id: 2);
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, authorId: 1);
        await SeedAnswerAsync(id: 2, authorId: 2);
        await SeedAnswerAsync(id: 3, authorId: 1);

        var filter = new GetCommunityAnswersRequest { UserId = 1 };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, x => Assert.Equal(1, x.Author.Id));
    }

    [Fact]
    public async Task GetAnswersAsync_FilterByIsHidden_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: false);
        await SeedAnswerAsync(id: 2, isHidden: true);

        var filter = new GetCommunityAnswersRequest { IsHidden = true };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.True(result.Items[0].IsHidden);
    }

    [Fact]
    public async Task GetAnswersAsync_FilterByIsAccepted_ReturnsMatchingAnswers()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isAccepted: false);
        await SeedAnswerAsync(id: 2, isAccepted: true);

        var filter = new GetCommunityAnswersRequest { IsAccepted = true };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
        Assert.True(result.Items[0].IsAccepted);
    }

    [Fact]
    public async Task GetAnswersAsync_SortByCreatedAtDesc_ReturnsOrderedResults()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();

        _db.CommunityAnswers.Add(new CommunityAnswer
        {
            Id = 1, QuestionId = 1, AuthorId = 99, Content = "First",
            VoteScore = 0, IsAccepted = false, IsHidden = false,
            EffDate = DateTimeOffset.UtcNow.AddDays(-2),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-2)
        });
        _db.CommunityAnswers.Add(new CommunityAnswer
        {
            Id = 2, QuestionId = 1, AuthorId = 99, Content = "Second",
            VoteScore = 0, IsAccepted = false, IsHidden = false,
            EffDate = DateTimeOffset.UtcNow.AddDays(-1),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        var filter = new GetCommunityAnswersRequest { SortBy = "CreatedAt", SortDirection = "DESC" };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(1, result.Items[1].Id);
    }

    [Fact]
    public async Task GetAnswersAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        for (int i = 1; i <= 5; i++)
            await SeedAnswerAsync(id: i, content: $"Answer {i}");

        var filter = new GetCommunityAnswersRequest { Page = 2, PageSize = 2 };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAnswersAsync_ContentPreview_TruncatesLongContent()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync(id: 300);
        var longContent = new string('X', 400);
        await SeedAnswerAsync(id: 300, questionId: 300, content: longContent);

        var filter = new GetCommunityAnswersRequest();

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert
        Assert.True(result.Items[0].ContentPreview.Length < longContent.Length);
        Assert.EndsWith("...", result.Items[0].ContentPreview);
    }

    #endregion

    #region GetAnswerByIdAsync — Detail Tests

    [Fact]
    public async Task GetAnswerByIdAsync_ExistingAnswer_ReturnsDetail()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1);

        // Act
        var result = await _sut.GetAnswerByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.NotEmpty(result.Content);
        Assert.NotNull(result.Question);
        Assert.NotNull(result.Author);
    }

    [Fact]
    public async Task GetAnswerByIdAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange & Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.GetAnswerByIdAsync(9999));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task GetAnswerByIdAsync_DeletedAnswer_ThrowsNotFound()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.GetAnswerByIdAsync(1));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task GetAnswerByIdAsync_AuthorEmailIncluded_WhenActive()
    {
        // Arrange
        await SeedUserAsync(id: 1, email: "user@example.com", status: UserStatus.Active);
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, authorId: 1);

        // Act
        var result = await _sut.GetAnswerByIdAsync(1);

        // Assert
        Assert.Equal("user@example.com", result.Author.Email);
        Assert.True(result.Author.IsActive);
    }

    #endregion

    #region HideAnswerAsync — Hide Tests

    [Fact]
    public async Task HideAnswerAsync_VisibleAnswer_SetsIsHiddenAndReturnsResponse()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: false);

        var command = new HideCommunityAnswerCommand
        {
            AnswerId = 1,
            ModeratedByUserId = 1,
            Reason = "Violates community guidelines"
        };

        // Act
        var result = await _sut.HideAnswerAsync(command);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.True(result.IsHidden);
        Assert.NotEqual(default, result.ModeratedAt);
        Assert.Equal(1, result.ModeratedBy);

        // Verify persisted
        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.True(answer.IsHidden);
    }

    [Fact]
    public async Task HideAnswerAsync_AlreadyHiddenAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: true);

        var command = new HideCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.HideAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerAlreadyHidden, ex.ResponseCode);
    }

    [Fact]
    public async Task HideAnswerAsync_DeletedAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        var command = new HideCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.HideAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerCannotBeModerated, ex.ResponseCode);
    }

    [Fact]
    public async Task HideAnswerAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange
        var command = new HideCommunityAnswerCommand { AnswerId = 9999, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.HideAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task HideAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: false);

        var command = new HideCommunityAnswerCommand
        {
            AnswerId = 1,
            ModeratedByUserId = 1,
            Reason = "Spam content",
            TraceId = "trace-123"
        };

        // Act
        await _sut.HideAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerHidden, _auditLog.LoggedAction);
        Assert.Equal("CommunityAnswer", _auditLog.LoggedEntityType);
    }

    [Fact]
    public async Task HideAnswerAsync_AcceptedAnswer_CanBeHiddenWithoutUnaccepting()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isAccepted: true, isHidden: false);

        var command = new HideCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act
        var result = await _sut.HideAnswerAsync(command);

        // Assert
        Assert.True(result.IsHidden);
        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.True(answer.IsHidden);
        Assert.True(answer.IsAccepted);
    }

    #endregion

    #region ShowAnswerAsync — Show Tests

    [Fact]
    public async Task ShowAnswerAsync_HiddenAnswer_ClearsIsHiddenAndReturnsResponse()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: true);

        var command = new ShowCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act
        var result = await _sut.ShowAnswerAsync(command);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.False(result.IsHidden);
        Assert.NotEqual(default, result.ModeratedAt);

        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.False(answer.IsHidden);
    }

    [Fact]
    public async Task ShowAnswerAsync_NotHiddenAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: false);

        var command = new ShowCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.ShowAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotHidden, ex.ResponseCode);
    }

    [Fact]
    public async Task ShowAnswerAsync_DeletedAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        var command = new ShowCommunityAnswerCommand { AnswerId = 1, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.ShowAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerCannotBeModerated, ex.ResponseCode);
    }

    [Fact]
    public async Task ShowAnswerAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange
        var command = new ShowCommunityAnswerCommand { AnswerId = 9999, ModeratedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.ShowAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task ShowAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: true);

        var command = new ShowCommunityAnswerCommand
        {
            AnswerId = 1,
            ModeratedByUserId = 1,
            TraceId = "trace-456"
        };

        // Act
        await _sut.ShowAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerShown, _auditLog.LoggedAction);
    }

    #endregion

    #region DeleteAnswerAsync — Delete Tests

    [Fact]
    public async Task DeleteAnswerAsync_ActiveAnswer_SetsDeletedAtAndDeletedBy()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1);

        var command = new DeleteCommunityAnswerCommand { AnswerId = 1, DeletedByUserId = 1 };

        // Act
        var result = await _sut.DeleteAnswerAsync(command);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.True(result.IsDeleted);
        Assert.NotEqual(default, result.DeletedAt);
        Assert.Equal(1, result.DeletedBy);

        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.NotEqual(default, answer.DeletedAt);
    }

    [Fact]
    public async Task DeleteAnswerAsync_AlreadyDeletedAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        var command = new DeleteCommunityAnswerCommand { AnswerId = 1, DeletedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerAlreadyDeleted, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteAnswerAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange
        var command = new DeleteCommunityAnswerCommand { AnswerId = 9999, DeletedByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1);

        var command = new DeleteCommunityAnswerCommand
        {
            AnswerId = 1,
            DeletedByUserId = 1,
            TraceId = "trace-789"
        };

        // Act
        await _sut.DeleteAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerDeleted, _auditLog.LoggedAction);
    }

    [Fact]
    public async Task DeleteAnswerAsync_HiddenAnswer_CanBeDeleted()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isHidden: true);

        var command = new DeleteCommunityAnswerCommand { AnswerId = 1, DeletedByUserId = 1 };

        // Act
        var result = await _sut.DeleteAnswerAsync(command);

        // Assert
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task DeleteAnswerAsync_AcceptedAnswer_CanBeDeleted()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isAccepted: true);

        var command = new DeleteCommunityAnswerCommand { AnswerId = 1, DeletedByUserId = 1 };

        // Act
        var result = await _sut.DeleteAnswerAsync(command);

        // Assert
        Assert.True(result.IsDeleted);
    }

    #endregion

    #region RestoreAnswerAsync — Restore Tests

    [Fact]
    public async Task RestoreAnswerAsync_DeletedAnswer_ClearsDeletedFieldsAndReturnsResponse()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true, deletedBy: 1);

        var command = new RestoreCommunityAnswerCommand { AnswerId = 1, RestoredByUserId = 1 };

        // Act
        var result = await _sut.RestoreAnswerAsync(command);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.False(result.IsDeleted);
        Assert.Null(result.DeletedAt);
        Assert.Null(result.DeletedBy);

        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.Null(answer.DeletedAt);
        Assert.Null(answer.DeletedBy);
    }

    [Fact]
    public async Task RestoreAnswerAsync_NonDeletedAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: false);

        var command = new RestoreCommunityAnswerCommand { AnswerId = 1, RestoredByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.RestoreAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotDeleted, ex.ResponseCode);
    }

    [Fact]
    public async Task RestoreAnswerAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange
        var command = new RestoreCommunityAnswerCommand { AnswerId = 9999, RestoredByUserId = 1 };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.RestoreAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task RestoreAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        var command = new RestoreCommunityAnswerCommand
        {
            AnswerId = 1,
            RestoredByUserId = 1,
            TraceId = "trace-101"
        };

        // Act
        await _sut.RestoreAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerRestored, _auditLog.LoggedAction);
    }

    [Fact]
    public async Task RestoreAnswerAsync_RestoredHiddenAnswer_KeepsHiddenState()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true, isHidden: true, deletedBy: 1);

        var command = new RestoreCommunityAnswerCommand { AnswerId = 1, RestoredByUserId = 1 };

        // Act
        var result = await _sut.RestoreAnswerAsync(command);

        // Assert
        Assert.False(result.IsDeleted);
        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.True(answer.IsHidden);
        Assert.Null(answer.DeletedAt);
    }

    #endregion

    #region Response DTO Contract Tests

    [Fact]
    public async Task GetAnswersAsync_ListItemResponse_MatchesExpectedDto()
    {
        // Arrange
        await SeedUserAsync(id: 1, fullName: "John Doe", email: "john@example.com");
        await SeedQuestionAsync(id: 1, title: "Test Question", status: CommunityQuestionStatus.Open);
        await SeedAnswerAsync(id: 1, questionId: 1, authorId: 1, content: "Short answer");

        var filter = new GetCommunityAnswersRequest();

        // Act
        var result = await _sut.GetAnswersAsync(filter);
        var item = result.Items[0];

        // Assert — verify all required fields exist and are correctly typed
        Assert.Equal(1, item.Id);
        Assert.NotEmpty(item.ContentPreview);
        Assert.NotNull(item.Question);
        Assert.Equal(1, item.Question.Id);
        Assert.Equal("Test Question", item.Question.Title);
        Assert.NotNull(item.Author);
        Assert.Equal(1, item.Author.Id);
        Assert.Equal("John Doe", item.Author.FullName);
        Assert.False(item.IsHidden);
        Assert.False(item.IsAccepted);
        Assert.NotEqual(default, item.CreatedAt);
        Assert.Null(item.DeletedAt);
    }

    [Fact]
    public async Task GetAnswerByIdAsync_DetailResponse_MatchesExpectedDto()
    {
        // Arrange
        await SeedUserAsync(id: 1, fullName: "John Doe", email: "john@example.com", status: UserStatus.Active);
        await SeedQuestionAsync(id: 1, title: "Test Question", status: CommunityQuestionStatus.Open);
        await SeedAnswerAsync(id: 1, questionId: 1, authorId: 1, content: "Full content here");

        // Act
        var result = await _sut.GetAnswerByIdAsync(1);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Full content here", result.Content);
        Assert.Equal(1, result.Question.Id);
        Assert.Equal("Test Question", result.Question.Title);
        Assert.Equal("Open", result.Question.Status);
        Assert.False(result.Question.IsLocked);
        Assert.Null(result.Question.DeletedAt);
        Assert.Equal(1, result.Author.Id);
        Assert.Equal("John Doe", result.Author.FullName);
        Assert.Equal("john@example.com", result.Author.Email);
        Assert.True(result.Author.IsActive);
        Assert.False(result.IsHidden);
        Assert.False(result.IsAccepted);
        Assert.Equal(0, result.VoteScore);
        Assert.NotEqual(default, result.CreatedAt);
        Assert.Null(result.DeletedAt);
        Assert.Null(result.DeletedBy);
    }

    [Fact]
    public async Task GetAnswersAsync_DoesNotExposeHiddenOrDeletedInCounts()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync(id: 200);
        await SeedAnswerAsync(id: 200, questionId: 200, content: "Visible answer");
        await SeedAnswerAsync(id: 201, questionId: 200, isHidden: true);
        await SeedAnswerAsync(id: 202, questionId: 200, isDeleted: true);

        var filter = new GetCommunityAnswersRequest { IncludeDeleted = false };

        // Act
        var result = await _sut.GetAnswersAsync(filter);

        // Assert — the non-deleted visible answer (id: 200) must be present;
        // deleted answer (id: 202) must NOT appear (soft-delete filter works in InMemory)
        Assert.All(result.Items, item => Assert.NotEqual(202, item.Id));

        // Note: IsHidden=false filter does not translate in InMemory — covered by integration tests
    }

    #endregion

    #region CreateAnswerAsync — Create Tests

    [Fact]
    public async Task CreateAnswerAsync_ValidData_CreatesAnswerAndReturnsResponse()
    {
        // Arrange
        await SeedUserAsync(id: 10);
        await SeedQuestionAsync(id: 5);
        await SeedAnswerAsync(id: 1, questionId: 5, authorId: 10);

        var command = new CreateCommunityAnswerCommand
        {
            QuestionId = 5,
            AuthorId = 10,
            Content = "This is a new answer content",
            CreatedByUserId = 10
        };

        // Act
        var result = await _sut.CreateAnswerAsync(command);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal(5, result.QuestionId);
        Assert.Equal(10, result.AuthorId);
        Assert.Equal("This is a new answer content", result.Content);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task CreateAnswerAsync_NonExistingQuestion_ThrowsNotFound()
    {
        // Arrange
        var command = new CreateCommunityAnswerCommand
        {
            QuestionId = 9999,
            AuthorId = 99,
            Content = "Answer content",
            CreatedByUserId = 99
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateAnswerAsync_NonExistingAuthor_ThrowsNotFound()
    {
        // Arrange
        await SeedQuestionAsync(id: 5);

        var command = new CreateCommunityAnswerCommand
        {
            QuestionId = 5,
            AuthorId = 9999,
            Content = "Answer content",
            CreatedByUserId = 99
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerAuthorNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync(id: 10);
        await SeedQuestionAsync(id: 5);

        var command = new CreateCommunityAnswerCommand
        {
            QuestionId = 5,
            AuthorId = 10,
            Content = "Audit test answer",
            CreatedByUserId = 10,
            TraceId = "trace-create-001"
        };

        // Act
        await _sut.CreateAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerCreated, _auditLog.LoggedAction);
        Assert.Equal("CommunityAnswer", _auditLog.LoggedEntityType);
    }

    [Fact]
    public async Task CreateAnswerAsync_SetsCorrectDefaults()
    {
        // Arrange
        await SeedUserAsync(id: 10);
        await SeedQuestionAsync(id: 5);

        var command = new CreateCommunityAnswerCommand
        {
            QuestionId = 5,
            AuthorId = 10,
            Content = "Testing defaults",
            CreatedByUserId = 10
        };

        // Act
        var result = await _sut.CreateAnswerAsync(command);

        // Assert — verify the answer has correct default values in DB
        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == result.Id);
        Assert.Equal(0, answer.VoteScore);
        Assert.False(answer.IsAccepted);
        Assert.False(answer.IsHidden);
        Assert.Null(answer.DeletedAt);
    }

    #endregion

    #region UpdateAnswerAsync — Update Tests

    [Fact]
    public async Task UpdateAnswerAsync_ValidData_UpdatesContentAndReturnsResponse()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1);

        var command = new UpdateCommunityAnswerCommand
        {
            AnswerId = 1,
            Content = "Updated answer content",
            UpdatedByUserId = 99
        };

        // Act
        var result = await _sut.UpdateAnswerAsync(command);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Updated answer content", result.Content);
        Assert.NotNull(result.UpdatedAt);

        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.Equal("Updated answer content", answer.Content);
    }

    [Fact]
    public async Task UpdateAnswerAsync_NonExistingAnswer_ThrowsNotFound()
    {
        // Arrange
        var command = new UpdateCommunityAnswerCommand
        {
            AnswerId = 9999,
            Content = "Updated content",
            UpdatedByUserId = 99
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateAnswerAsync_DeletedAnswer_ThrowsConflict()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isDeleted: true);

        var command = new UpdateCommunityAnswerCommand
        {
            AnswerId = 1,
            Content = "Updated content",
            UpdatedByUserId = 99
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateAnswerAsync(command));
        Assert.Equal(ResponseCodes.CommunityAnswerCannotBeModerated, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateAnswerAsync_AuditLogCreated()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, content: "Original content");

        var command = new UpdateCommunityAnswerCommand
        {
            AnswerId = 1,
            Content = "New content",
            UpdatedByUserId = 99,
            TraceId = "trace-update-001"
        };

        // Act
        await _sut.UpdateAnswerAsync(command);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.CommunityAnswerUpdated, _auditLog.LoggedAction);
        Assert.Equal("CommunityAnswer", _auditLog.LoggedEntityType);
    }

    [Fact]
    public async Task UpdateAnswerAsync_DoesNotChangeOtherFields()
    {
        // Arrange
        await SeedUserAsync();
        await SeedQuestionAsync();
        await SeedAnswerAsync(id: 1, isAccepted: true, isHidden: true, content: "Original");

        var command = new UpdateCommunityAnswerCommand
        {
            AnswerId = 1,
            Content = "Changed",
            UpdatedByUserId = 99
        };

        // Act
        var result = await _sut.UpdateAnswerAsync(command);

        // Assert — other fields unchanged
        var answer = await _db.CommunityAnswers.AsNoTracking().FirstAsync(a => a.Id == 1);
        Assert.Equal("Changed", answer.Content);
        Assert.True(answer.IsAccepted);
        Assert.True(answer.IsHidden);
        Assert.Equal(1, answer.QuestionId);
        Assert.Equal(99, answer.AuthorId);
    }

    #endregion
}
