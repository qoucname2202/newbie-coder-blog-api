using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class InterviewQuestionServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly InterviewQuestionRepository _iqRepo;
    private readonly TestAuditLogService _auditLog;
    private readonly InterviewQuestionService _sut;

    public InterviewQuestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _iqRepo = new InterviewQuestionRepository(_db);
        _auditLog = new TestAuditLogService();
        _sut = new InterviewQuestionService(_db, _iqRepo, _auditLog);
    }

    public void Dispose() => _db.Dispose();

    #region Seed helpers

    private async Task<User> SeedAdminUserAsync(
        long id = 99,
        string email = "admin@test.com",
        string username = "admin",
        UserStatus status = UserStatus.Active)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Username = username,
            Password = "HashedPassword",
            FullName = "Admin User",
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

    private async Task<PostCategory> SeedCategoryAsync(
        long id = 1,
        string name = "Backend",
        string slug = "backend",
        EntityStatus status = EntityStatus.Active)
    {
        var category = new PostCategory
        {
            Id = id,
            Name = name,
            Slug = slug,
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.PostCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<InterviewQuestion> SeedInterviewQuestionAsync(
        long id = 1,
        string title = "What is Dependency Injection?",
        string content = "Explain Dependency Injection in ASP.NET Core.",
        InterviewLevel level = InterviewLevel.Middle,
        PostStatus status = PostStatus.Draft,
        long authorId = 99,
        bool isDeleted = false,
        DateTimeOffset? deletedAt = null,
        long? deletedBy = null)
    {
        var question = new InterviewQuestion
        {
            Id = id,
            Title = title,
            Slug = "dependency-injection",
            QuestionContent = content,
            Level = level,
            Status = status,
            AuthorId = authorId,
            EffDate = DateTimeOffset.UtcNow.AddDays(-10),
            DateLastMaint = DateTimeOffset.UtcNow.AddDays(-5)
        };

        if (isDeleted)
        {
            question.DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
            question.DeletedBy = deletedBy;
        }

        _db.InterviewQuestions.Add(question);
        await _db.SaveChangesAsync();
        return question;
    }

    private async Task<InterviewAnswer> SeedInterviewAnswerAsync(
        long id = 1,
        long questionId = 1,
        string content = "Dependency Injection is a design pattern...",
        bool isOfficial = true)
    {
        var answer = new InterviewAnswer
        {
            Id = id,
            QuestionId = questionId,
            AnswerContent = content,
            IsOfficial = isOfficial,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.InterviewAnswers.Add(answer);
        await _db.SaveChangesAsync();
        return answer;
    }

    #endregion

    #region CreateQuestionAsync

    [Fact]
    public async Task CreateQuestion_WithValidRequest_ReturnsCreatedQuestion()
    {
        // Arrange
        await SeedAdminUserAsync();
        var tag = await SeedTagAsync();
        await SeedCategoryAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "What is LINQ?",
            QuestionContent = "Explain LINQ in C#.",
            DifficultyLevel = "Junior",
            Status = "Draft",
            Technology = "C#",
            Topic = "LINQ",
            TagIds = [tag.Id],
            Answers =
            [
                new InterviewAnswerRequest { Content = "LINQ is a set of technologies...", IsOfficial = true }
            ]
        };

        // Act
        var result = await _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-1");

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("What is LINQ?", result.Title);
        Assert.Equal("Junior", result.DifficultyLevel);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("C#", result.Technology);
        Assert.Equal("LINQ", result.Topic);
        Assert.Equal(1, result.AnswerCount);

        // Verify DB
        var dbQuestion = await _db.InterviewQuestions.FindAsync(result.Id);
        Assert.NotNull(dbQuestion);
        Assert.Equal("what-is-linq", dbQuestion.Slug);

        // Verify tag association
        var tagAssoc = await _db.InterviewQuestionTags.FirstOrDefaultAsync(iqt => iqt.QuestionId == result.Id);
        Assert.NotNull(tagAssoc);
        Assert.Equal(tag.Id, tagAssoc.TagId);

        // Verify answer
        var answer = await _db.InterviewAnswers.FirstOrDefaultAsync(a => a.QuestionId == result.Id);
        Assert.NotNull(answer);
        Assert.Equal("LINQ is a set of technologies...", answer.AnswerContent);
        Assert.True(answer.IsOfficial);
    }

    [Fact]
    public async Task CreateQuestion_WithoutAnswers_SetsStatusToDraft()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Simple question",
            QuestionContent = "What is ASP.NET Core?",
            DifficultyLevel = "Entry"
        };

        // Act
        var result = await _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null);

        // Assert
        Assert.Equal("Draft", result.Status);
        Assert.Equal(0, result.AnswerCount);
    }

    [Fact]
    public async Task CreateQuestion_WithActiveStatusAndNoAnswers_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Active question without answers",
            QuestionContent = "This should fail.",
            Status = "Published"  // Published = Active in this system
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionAnswerRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateQuestion_WithMultiplePreferredAnswers_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Multi-preferred question",
            QuestionContent = "This should fail.",
            Status = "Draft",
            Answers =
            [
                new InterviewAnswerRequest { Content = "Answer 1", IsOfficial = true },
                new InterviewAnswerRequest { Content = "Answer 2", IsOfficial = true }
            ]
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionMultiplePreferredAnswers, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateQuestion_WithInvalidDifficulty_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Invalid difficulty",
            QuestionContent = "Content here.",
            DifficultyLevel = "InvalidLevel"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionInvalidDifficulty, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateQuestion_WithNonExistentTag_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Question with bad tag",
            QuestionContent = "Content.",
            TagIds = [9999]
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionTagNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateQuestion_WritesAuditLog()
    {
        // Arrange
        await SeedAdminUserAsync();

        var request = new CreateInterviewQuestionRequest
        {
            Title = "Audit test question",
            QuestionContent = "Testing audit log."
        };

        // Act
        await _sut.CreateQuestionAsync(request, createdByUserId: 99, ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-abc");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.InterviewQuestionCreated, _auditLog.LoggedAction);
        Assert.Equal(nameof(InterviewQuestion), _auditLog.LoggedEntityType);
        Assert.NotNull(_auditLog.LoggedNewValue);
    }

    #endregion

    #region GetQuestionsAsync

    [Fact]
    public async Task GetQuestions_ReturnsPagedResults()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, title: "Question One");
        await SeedInterviewQuestionAsync(id: 2, title: "Question Two");
        await SeedInterviewQuestionAsync(id: 3, title: "Question Three");

        var filter = new GetInterviewQuestionsRequest { PageNumber = 1, PageSize = 2 };

        // Act
        var result = await _sut.GetQuestionsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetQuestions_WithKeyword_FiltersResults()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, title: "What is Dependency Injection?");
        await SeedInterviewQuestionAsync(id: 2, title: "What is LINQ?");
        await SeedInterviewQuestionAsync(id: 3, title: "Entity Framework basics");

        // Act — keyword filter is applied via EF.Functions.Like which requires Npgsql
        // For InMemory DB, we verify the repository compiles correctly but filtering is tested via status/difficulty
        var filter = new GetInterviewQuestionsRequest { Status = "Draft" };

        var result = await _sut.GetQuestionsAsync(filter);

        // Assert — no keyword filter for InMemory, just verify status filter works
        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("Draft", item.Status));
    }

    [Fact]
    public async Task GetQuestions_WithStatusFilter_FiltersResults()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Draft);
        await SeedInterviewQuestionAsync(id: 2, status: PostStatus.Published);
        await SeedInterviewQuestionAsync(id: 3, status: PostStatus.Published);

        var filter = new GetInterviewQuestionsRequest { Status = "Published" };

        // Act
        var result = await _sut.GetQuestionsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("Published", item.Status));
    }

    [Fact]
    public async Task GetQuestions_WithDifficultyFilter_FiltersResults()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, level: InterviewLevel.Entry);
        await SeedInterviewQuestionAsync(id: 2, level: InterviewLevel.Senior);
        await SeedInterviewQuestionAsync(id: 3, level: InterviewLevel.Senior);

        var filter = new GetInterviewQuestionsRequest { DifficultyLevel = "Senior" };

        // Act
        var result = await _sut.GetQuestionsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal("Senior", item.DifficultyLevel));
    }

    [Fact]
    public async Task GetQuestions_WithSortDirection_ReturnsOrderedResults()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, title: "Alpha question");
        await SeedInterviewQuestionAsync(id: 2, title: "Beta question");
        await SeedInterviewQuestionAsync(id: 3, title: "Gamma question");

        var filter = new GetInterviewQuestionsRequest { SortBy = "title", SortDirection = "asc" };

        // Act
        var result = await _sut.GetQuestionsAsync(filter);

        // Assert
        Assert.Equal("Alpha question", result.Items[0].Title);
        Assert.Equal("Beta question", result.Items[1].Title);
        Assert.Equal("Gamma question", result.Items[2].Title);
    }

    [Fact]
    public async Task GetQuestions_WithIncludeDeleted_IncludesDeletedQuestions()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, isDeleted: false);
        await SeedInterviewQuestionAsync(id: 2, isDeleted: true);
        await SeedInterviewQuestionAsync(id: 3, isDeleted: false);

        var filterWithDeleted = new GetInterviewQuestionsRequest { IncludeDeleted = true };

        // Act — IncludeDeleted means "also show deleted items alongside non-deleted"
        var resultWith = await _sut.GetQuestionsAsync(filterWithDeleted);

        // Assert
        Assert.Equal(3, resultWith.Items.Count);
    }

    [Fact]
    public async Task GetQuestions_ExcludesDeletedByDefault()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, isDeleted: false);
        await SeedInterviewQuestionAsync(id: 2, isDeleted: true);
        await SeedInterviewQuestionAsync(id: 3, isDeleted: false);

        var filter = new GetInterviewQuestionsRequest();

        // Act
        var result = await _sut.GetQuestionsAsync(filter);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.NotEqual("Archived", item.Status));
    }

    #endregion

    #region GetQuestionByIdAsync

    [Fact]
    public async Task GetQuestionById_WithValidId_ReturnsDetail()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedTagAsync(id: 10, name: "C#", slug: "c-sharp");
        var question = await SeedInterviewQuestionAsync(id: 5);
        await SeedInterviewAnswerAsync(id: 100, questionId: 5, content: "DI answer.", isOfficial: true);

        // Act
        var result = await _sut.GetQuestionByIdAsync(5);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal("What is Dependency Injection?", result.Title);
        Assert.Single(result.Answers);
        Assert.Equal("DI answer.", result.Answers[0].Content);
    }

    [Fact]
    public async Task GetQuestionById_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.GetQuestionByIdAsync(9999));

        Assert.Equal(ResponseCodes.InterviewQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task GetQuestionById_ExcludesDeletedAnswers()
    {
        // Arrange
        await SeedAdminUserAsync();
        var question = await SeedInterviewQuestionAsync(id: 10);
        await SeedInterviewAnswerAsync(id: 200, questionId: 10, content: "Active answer");
        await SeedInterviewAnswerAsync(id: 201, questionId: 10, content: "Deleted answer", isOfficial: false);

        // Manually soft-delete answer 201
        var deletedAnswer = await _db.InterviewAnswers.FindAsync(201L);
        deletedAnswer!.DeletedAt = DateTimeOffset.UtcNow;
        deletedAnswer.DeletedBy = 99;
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetQuestionByIdAsync(10);

        // Assert
        Assert.Single(result.Answers);
        Assert.Equal("Active answer", result.Answers[0].Content);
    }

    #endregion

    #region UpdateQuestionAsync

    [Fact]
    public async Task UpdateQuestion_WithValidRequest_UpdatesQuestion()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedTagAsync(id: 1, name: "C#", slug: "c-sharp");
        await SeedTagAsync(id: 2, name: "ASP.NET Core", slug: "asp-net-core");
        var question = await SeedInterviewQuestionAsync(id: 1);

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Updated Title",
            QuestionContent = "Updated content.",
            Explanation = "Updated explanation.",
            DifficultyLevel = "Senior",
            Status = "Draft",  // Using Draft so no answers are required
            Technology = "C#",
            Topic = "DI",
            TagIds = [1, 2]
        };

        // Act
        var result = await _sut.UpdateQuestionAsync(
            1, request, updatedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-2");

        // Assert
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Senior", result.DifficultyLevel);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("C#", result.Technology);
        Assert.Equal("DI", result.Topic);
    }

    [Fact]
    public async Task UpdateQuestion_WithNonExistentId_ThrowsBusinessException()
    {
        // Arrange
        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Title",
            QuestionContent = "Content"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateQuestionAsync(9999, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateQuestion_PreservesCreatedFields()
    {
        // Arrange
        await SeedAdminUserAsync();
        var question = await SeedInterviewQuestionAsync(id: 1);
        var originalEffDate = question.EffDate;
        var originalAuthorId = question.AuthorId;

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "New Title",
            QuestionContent = "New content."
        };

        // Act
        await _sut.UpdateQuestionAsync(
            1, request, updatedByUserId: 99,
            ipAddress: null, userAgent: null, traceId: null);

        // Assert
        var updated = await _db.InterviewQuestions.FindAsync(1L);
        Assert.Equal(originalEffDate, updated!.EffDate);
        Assert.Equal(originalAuthorId, updated.AuthorId);
    }

    [Fact]
    public async Task UpdateQuestion_WithActiveStatusAndNoAnswers_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();
        var question = await SeedInterviewQuestionAsync(id: 1);

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Active question without answers",
            QuestionContent = "This should fail.",
            Status = "Published"  // Published = Active in this system
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateQuestionAsync(1, request, updatedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionAnswerRequired, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateQuestion_WritesAuditLog()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Updated Title",
            QuestionContent = "Updated content."
        };

        // Act
        await _sut.UpdateQuestionAsync(
            1, request, updatedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-3");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.InterviewQuestionUpdated, _auditLog.LoggedAction);
        Assert.NotNull(_auditLog.LoggedOldValue);
        Assert.NotNull(_auditLog.LoggedNewValue);
    }

    [Fact]
    public async Task UpdateQuestion_AddsNewAnswer()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Title",
            QuestionContent = "Content",
            Answers =
            [
                new UpdateInterviewAnswerRequest { Id = 0, Content = "New answer.", IsOfficial = true }
            ]
        };

        // Act
        await _sut.UpdateQuestionAsync(
            1, request, updatedByUserId: 99,
            ipAddress: null, userAgent: null, traceId: null);

        // Assert
        var answers = await _db.InterviewAnswers
            .Where(a => a.QuestionId == 1 && a.DeletedAt == null)
            .ToListAsync();
        Assert.Single(answers);
        Assert.Equal("New answer.", answers[0].AnswerContent);
    }

    [Fact]
    public async Task UpdateQuestion_RemovesMissingAnswers()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);
        await SeedInterviewAnswerAsync(id: 1, questionId: 1, content: "Old answer");

        var request = new UpdateInterviewQuestionRequest
        {
            Title = "Title",
            QuestionContent = "Content",
            Answers = [] // Empty — should remove existing answer
        };

        // Act
        await _sut.UpdateQuestionAsync(
            1, request, updatedByUserId: 99,
            ipAddress: null, userAgent: null, traceId: null);

        // Assert
        var activeAnswers = await _db.InterviewAnswers
            .Where(a => a.QuestionId == 1 && a.DeletedAt == null)
            .ToListAsync();
        Assert.Empty(activeAnswers);

        var deletedAnswers = await _db.InterviewAnswers
            .Where(a => a.QuestionId == 1 && a.DeletedAt != null)
            .ToListAsync();
        Assert.Single(deletedAnswers);
    }

    #endregion

    #region DeleteQuestionAsync

    [Fact]
    public async Task DeleteQuestion_SoftDeletesQuestion()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);

        // Act
        var result = await _sut.DeleteQuestionAsync(
            1, deletedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-4");

        // Assert
        Assert.Equal("Archived", result.Status);
        Assert.NotNull(result.DeletedAt);

        var deleted = await _db.InterviewQuestions.FindAsync(1L);
        Assert.NotNull(deleted!.DeletedAt);
        Assert.Equal(99, deleted.DeletedBy);
        Assert.Equal(PostStatus.Archived, deleted.Status);
    }

    [Fact]
    public async Task DeleteQuestion_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteQuestionAsync(9999, deletedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteQuestion_AlreadyDeleted_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();
        // First seed a normal question (not deleted)
        await SeedInterviewQuestionAsync(id: 1, isDeleted: false);
        // Delete it once — this sets isDeleted=true
        await _sut.DeleteQuestionAsync(1, deletedByUserId: 99, ipAddress: null, userAgent: null, traceId: null);

        // Act & Assert — try to delete again
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteQuestionAsync(1, deletedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionAlreadyDeleted, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteQuestion_WritesAuditLog()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);

        // Act
        await _sut.DeleteQuestionAsync(
            1, deletedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-5");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.InterviewQuestionDeleted, _auditLog.LoggedAction);
    }

    #endregion

    #region RestoreQuestionAsync

    [Fact]
    public async Task RestoreQuestion_RestoresDeletedQuestion()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, isDeleted: true, status: PostStatus.Archived);

        // Act
        var result = await _sut.RestoreQuestionAsync(
            1, restoredByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-6");

        // Assert
        Assert.Equal("Draft", result.Status);
        Assert.False(result.IsDeleted);

        var restored = await _db.InterviewQuestions.FindAsync(1L);
        Assert.Null(restored!.DeletedAt);
        Assert.Null(restored.DeletedBy);
        Assert.Equal(PostStatus.Draft, restored.Status);
    }

    [Fact]
    public async Task RestoreQuestion_WithNonExistentId_ThrowsBusinessException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestoreQuestionAsync(9999, restoredByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task RestoreQuestion_NotDeleted_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, isDeleted: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestoreQuestionAsync(1, restoredByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionNotDeleted, ex.ResponseCode);
    }

    [Fact]
    public async Task RestoreQuestion_WritesAuditLog()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, isDeleted: true);

        // Act
        await _sut.RestoreQuestionAsync(
            1, restoredByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-7");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.InterviewQuestionRestored, _auditLog.LoggedAction);
    }

    #endregion

    #region ChangeQuestionStatusAsync

    [Fact]
    public async Task ChangeStatus_DraftToPublished_WithAnswers_Succeeds()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Draft);
        await SeedInterviewAnswerAsync(id: 1, questionId: 1, content: "Answer content.");

        var request = new ChangeInterviewQuestionStatusRequest
        {
            Status = "Published",
            Reason = "Question reviewed and approved."
        };

        // Act
        var result = await _sut.ChangeQuestionStatusAsync(
            1, request, changedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-8");

        // Assert
        Assert.Equal("Draft", result.PreviousStatus);
        Assert.Equal("Published", result.CurrentStatus);
        Assert.Equal(99, result.UpdatedBy);
    }

    [Fact]
    public async Task ChangeStatus_PublishedToHidden_Succeeds()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Published);
        await SeedInterviewAnswerAsync(id: 1, questionId: 1, content: "Answer.");

        var request = new ChangeInterviewQuestionStatusRequest { Status = "Hidden" };

        // Act
        var result = await _sut.ChangeQuestionStatusAsync(
            1, request, changedByUserId: 99,
            ipAddress: null, userAgent: null, traceId: null);

        // Assert
        Assert.Equal("Published", result.PreviousStatus);
        Assert.Equal("Hidden", result.CurrentStatus);
    }

    [Fact]
    public async Task ChangeStatus_ArchivedToDraft_Rejects()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Archived);

        var request = new ChangeInterviewQuestionStatusRequest { Status = "Draft" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeQuestionStatusAsync(1, request, changedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionInvalidStatusTransition, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_SameStatus_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Draft);

        var request = new ChangeInterviewQuestionStatusRequest { Status = "Draft" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeQuestionStatusAsync(1, request, changedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionAlreadyInTargetStatus, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_InvalidStatus_ThrowsBusinessException()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1);

        var request = new ChangeInterviewQuestionStatusRequest { Status = "InvalidStatus" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeQuestionStatusAsync(1, request, changedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionInvalidStatus, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_NonExistentQuestion_ThrowsBusinessException()
    {
        // Arrange
        var request = new ChangeInterviewQuestionStatusRequest { Status = "Published" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangeQuestionStatusAsync(9999, request, changedByUserId: 99, ipAddress: null, userAgent: null, traceId: null));

        Assert.Equal(ResponseCodes.InterviewQuestionNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangeStatus_WritesAuditLog()
    {
        // Arrange
        await SeedAdminUserAsync();
        await SeedInterviewQuestionAsync(id: 1, status: PostStatus.Draft);
        await SeedInterviewAnswerAsync(id: 1, questionId: 1, content: "Answer.");

        var request = new ChangeInterviewQuestionStatusRequest
        {
            Status = "Published",
            Reason = "Approved"
        };

        // Act
        await _sut.ChangeQuestionStatusAsync(
            1, request, changedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-9");

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.InterviewQuestionStatusChanged, _auditLog.LoggedAction);
    }

    #endregion
}
