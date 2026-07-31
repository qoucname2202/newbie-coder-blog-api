using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.CQRS.Posts;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.CQRS.Posts;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class AdminPostServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PostRepository _postRepo;
    private readonly TestAuditLogService _auditLog;
    private readonly AdminPostService _sut;

    public AdminPostServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _postRepo = new PostRepository(_db);
        _auditLog = new TestAuditLogService();
        var changeStatusHandler = new ChangePostStatusCommandHandler(_db, _postRepo, _auditLog);
        _sut = new AdminPostService(_db, _postRepo, _auditLog, changeStatusHandler);
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

    private async Task<User> SeedAuthorUserAsync(
        long id = 1,
        string email = "author@test.com",
        string username = "author",
        UserStatus status = UserStatus.Active)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Username = username,
            Password = "HashedPassword",
            FullName = "Author User",
            Location = "Test",
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<PostCategory> SeedCategoryAsync(
        long id = 1,
        string name = "Backend",
        EntityStatus status = EntityStatus.Active)
    {
        var category = new PostCategory
        {
            Id = id,
            Name = name,
            Slug = name.ToLowerInvariant(),
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.PostCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<Tag> SeedTagAsync(
        long id = 1,
        string name = "ASP.NET Core",
        EntityStatus status = EntityStatus.Active)
    {
        var tag = new Tag
        {
            Id = id,
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return tag;
    }

    private async Task<Post> SeedPostAsync(
        long id = 1,
        long authorId = 1,
        long? categoryId = null,
        PostStatus status = PostStatus.Draft,
        bool isDeleted = false,
        string title = "Test Post",
        string slug = "test-post")
    {
        var post = new Post
        {
            Id = id,
            AuthorId = authorId,
            CategoryId = categoryId,
            Title = title,
            Slug = slug,
            Summary = "Test summary",
            Content = "<p>Test content</p>",
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow,
            ViewCount = 10,
            CommentCount = 2
        };

        if (isDeleted)
        {
            post.DeletedAt = DateTimeOffset.UtcNow;
            post.DeletedBy = 99;
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        return post;
    }

    private async Task<PostTag> SeedPostTagAsync(long postId, long tagId)
    {
        var pt = new PostTag
        {
            PostId = postId,
            TagId = tagId,
            EffDate = DateTimeOffset.UtcNow
        };
        _db.PostTags.Add(pt);
        await _db.SaveChangesAsync();
        return pt;
    }

    #endregion

    #region GetPostsAsync — Success

    [Fact]
    public async Task GetPostsAsync_DefaultFilter_ReturnsPagedPosts()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedCategoryAsync(1, "Backend");
        await SeedPostAsync(1, authorId: 1, categoryId: 1, status: PostStatus.Published);
        await SeedPostAsync(2, authorId: 1, categoryId: 1, status: PostStatus.Draft);

        var filter = new GetAdminPostsRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetPostsAsync_ByKeyword_MatchesTitle()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, title: "ASP.NET Core Guide", slug: "asp-net-core-guide");
        await SeedPostAsync(2, authorId: 1, title: "React Tutorial", slug: "react-tutorial");

        var filter = new GetAdminPostsRequest { Keyword = "asp.net" };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Contains("ASP.NET", result.Items[0].Title);
    }

    [Fact]
    public async Task GetPostsAsync_ByKeyword_MatchesAuthorName()
    {
        var admin = await SeedAdminUserAsync(99);
        var author1 = await SeedAuthorUserAsync(1, email: "alice@test.com", username: "alice");
        var author2 = await SeedAuthorUserAsync(2, email: "bob@test.com", username: "bob");
        await SeedPostAsync(1, authorId: 1, slug: "post-alice");
        await SeedPostAsync(2, authorId: 2, slug: "post-bob");

        var filter = new GetAdminPostsRequest { Keyword = "alice" };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("alice", result.Items[0].Author?.Username);
    }

    [Fact]
    public async Task GetPostsAsync_ByStatus_FiltersCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);
        await SeedPostAsync(2, authorId: 1, status: PostStatus.Draft);

        var filter = new GetAdminPostsRequest { Status = "Published" };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Published", result.Items[0].Status);
    }

    [Fact]
    public async Task GetPostsAsync_ByAuthorId_FiltersCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author1 = await SeedAuthorUserAsync(1);
        var author2 = await SeedAuthorUserAsync(2, email: "author2@test.com", username: "author2");
        await SeedPostAsync(1, authorId: 1);
        await SeedPostAsync(2, authorId: 2);

        var filter = new GetAdminPostsRequest { AuthorId = 1 };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Author?.Id);
    }

    [Fact]
    public async Task GetPostsAsync_ByCategoryId_FiltersCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var cat1 = await SeedCategoryAsync(1, "Backend");
        var cat2 = await SeedCategoryAsync(2, "Frontend");
        await SeedPostAsync(1, authorId: 1, categoryId: 1);
        await SeedPostAsync(2, authorId: 1, categoryId: 2);

        var filter = new GetAdminPostsRequest { CategoryId = 1 };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Backend", result.Items[0].Category?.Name);
    }

    [Fact]
    public async Task GetPostsAsync_ByTagId_FiltersCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var tag1 = await SeedTagAsync(1, "ASP.NET");
        var tag2 = await SeedTagAsync(2, "React");
        var post1 = await SeedPostAsync(1, authorId: 1);
        var post2 = await SeedPostAsync(2, authorId: 1);
        await SeedPostTagAsync(1, 1);
        await SeedPostTagAsync(2, 2);

        var filter = new GetAdminPostsRequest { TagId = 1 };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPostsAsync_IncludeDeleted_ReturnsDeletedPosts()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true);

        var filter = new GetAdminPostsRequest { IncludeDeleted = true };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPostsAsync_ExcludeDeleted_ByDefault()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true);

        var filter = new GetAdminPostsRequest { IncludeDeleted = false };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetPostsAsync_Pagination_Works()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        for (int i = 1; i <= 5; i++)
            await SeedPostAsync(i, authorId: 1, title: $"Post {i}", slug: $"post-{i}");

        var filter = new GetAdminPostsRequest { PageNumber = 2, PageSize = 2 };

        var result = await _sut.GetPostsAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    #endregion

    #region GetPostByIdAsync

    [Fact]
    public async Task GetPostByIdAsync_ExistingPost_ReturnsDetail()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var cat = await SeedCategoryAsync(1, "Backend");
        var tag = await SeedTagAsync(1, "ASP.NET");
        var post = await SeedPostAsync(1, authorId: 1, categoryId: 1, title: "Test Post", slug: "test-post");
        await SeedPostTagAsync(1, 1);

        var result = await _sut.GetPostByIdAsync(1, CancellationToken.None);

        Assert.Equal(1, result.Id);
        Assert.Equal("Test Post", result.Title);
        Assert.Equal("test-post", result.Slug);
        Assert.Equal("Backend", result.Category?.Name);
        Assert.Single(result.Tags);
        Assert.Equal("ASP.NET", result.Tags[0].Name);
    }

    [Fact]
    public async Task GetPostByIdAsync_NonExistentPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.GetPostByIdAsync(9999, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    #endregion

    #region CreatePostAsync — Success

    [Fact]
    public async Task CreatePostAsync_ForCurrentAdmin_SetsAuthorAsAdmin()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Admin Post",
            Summary = "A post by admin",
            Content = "<p>Content</p>",
            Status = "Draft"
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-1",
            CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("Admin Post", result.Title);
        Assert.Equal("admin", result.Author?.Username);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task CreatePostAsync_ForSpecificAuthor_SetsAuthorCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1, email: "author@test.com", username: "author");
        var request = new CreateAdminPostRequest
        {
            Title = "Author Post",
            Content = "<p>Content</p>",
            AuthorId = 1,
            Status = "Draft"
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal(1, result.Author?.Id);
        Assert.Equal("author", result.Author?.Username);
    }

    [Fact]
    public async Task CreatePostAsync_WithoutAuthorId_SetsAdminAsAuthor()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Admin Post",
            Content = "<p>Content</p>",
            AuthorId = null,
            Status = "Draft"
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal(99, result.Author?.Id);
    }

    [Fact]
    public async Task CreatePostAsync_WithCategory_SetsCategoryCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedCategoryAsync(1, "Backend");
        var request = new CreateAdminPostRequest
        {
            Title = "Post with Category",
            Content = "<p>Content</p>",
            CategoryId = 1
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Backend", result.Category?.Name);
    }

    [Fact]
    public async Task CreatePostAsync_WithTags_SetsTagsCorrectly()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedTagAsync(1, "ASP.NET");
        await SeedTagAsync(2, "C#");
        var request = new CreateAdminPostRequest
        {
            Title = "Post with Tags",
            Content = "<p>Content</p>",
            TagIds = [1, 2]
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal(2, result.Tags.Count);
    }

    [Fact]
    public async Task CreatePostAsync_WithStatusPublished_SetsPublishedAt()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Published Post",
            Content = "<p>Content</p>",
            Status = "Published"
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Published", result.Status);
        Assert.NotNull(result.PublishedAt);
    }

    [Fact]
    public async Task CreatePostAsync_WritesAuditLog()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var request = new CreateAdminPostRequest
        {
            Title = "Audited Post",
            Content = "<p>Content</p>"
        };

        await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: "10.0.0.1",
            userAgent: "Browser",
            traceId: "trace-a",
            CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.PostCreatedByAdmin, _auditLog.LoggedAction);
        Assert.Equal(nameof(Post), _auditLog.LoggedEntityType);
    }

    #endregion

    #region CreatePostAsync — Validation Failures

    [Fact]
    public async Task CreatePostAsync_InvalidAuthorId_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            AuthorId = 9999
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.AuthorNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_InactiveAuthor_ThrowsBusinessException400()
    {
        var admin = await SeedAdminUserAsync(99);
        await SeedAuthorUserAsync(1, status: UserStatus.Inactive);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            AuthorId = 1
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.AuthorInactive, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_LockedAuthor_ThrowsBusinessException403()
    {
        var admin = await SeedAdminUserAsync(99);
        await SeedAuthorUserAsync(1, status: UserStatus.Locked);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            AuthorId = 1
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.AuthorLocked, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_InvalidCategoryId_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            CategoryId = 9999
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.CategoryNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_InvalidTagId_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            TagIds = [9999]
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.TagNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_DuplicateTagIds_ThrowsBusinessException400()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            TagIds = [1, 1]
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.InvalidTagIds, ex.ResponseCode);
    }

    [Fact]
    public async Task CreatePostAsync_InvalidStatus_ThrowsBusinessException400()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new CreateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            Status = "InvalidStatus"
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreatePostAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.InvalidPostStatus, ex.ResponseCode);
    }

    #endregion

    #region UpdatePostAsync — Success

    [Fact]
    public async Task UpdatePostAsync_ValidRequest_UpdatesPost()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, title: "Original Title", slug: "original-title");

        var request = new UpdateAdminPostRequest
        {
            Title = "Updated Title",
            Summary = "Updated summary",
            Content = "<p>Updated content</p>",
            Status = "Draft"
        };

        var result = await _sut.UpdatePostAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated summary", result.Summary);
        Assert.NotEqual(default, result.UpdatedAt);
    }

    [Fact]
    public async Task UpdatePostAsync_RegeneratesSlug_WhenTitleChanges()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, title: "Original", slug: "original");

        var request = new UpdateAdminPostRequest
        {
            Title = "Completely New Title Here",
            Content = "<p>Content</p>",
            Status = "Draft"
        };

        var result = await _sut.UpdatePostAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("completely-new-title-here", result.Slug);
    }

    [Fact]
    public async Task UpdatePostAsync_WritesAuditLog()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1);

        var request = new UpdateAdminPostRequest
        {
            Title = "Updated",
            Content = "<p>Updated</p>",
            Status = "Draft"
        };

        await _sut.UpdatePostAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: "10.0.0.1",
            userAgent: "Browser",
            traceId: "trace-u",
            CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.PostUpdatedByAdmin, _auditLog.LoggedAction);
        Assert.NotNull(_auditLog.LoggedOldValue);
        Assert.NotNull(_auditLog.LoggedNewValue);
    }

    [Fact]
    public async Task UpdatePostAsync_SetsPublishedAt_WhenTransitioningToPublished()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var post = await SeedPostAsync(1, authorId: 1, status: PostStatus.Draft);

        var request = new UpdateAdminPostRequest
        {
            Title = "Published Now",
            Content = "<p>Content</p>",
            Status = "Published"
        };

        var result = await _sut.UpdatePostAsync(
            1,
            request,
            updatedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Published", result.Status);
    }

    #endregion

    #region UpdatePostAsync — Validation Failures

    [Fact]
    public async Task UpdatePostAsync_NonExistentPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new UpdateAdminPostRequest
        {
            Title = "Post",
            Content = "<p>Content</p>",
            Status = "Draft"
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdatePostAsync(9999, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    #endregion

    #region DeletePostAsync — Success

    [Fact]
    public async Task DeletePostAsync_ExistingPost_SoftDeletes()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1);

        var result = await _sut.DeletePostAsync(
            1,
            deletedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal(1, result.Id);
        Assert.Equal("Deleted", result.Status);
        Assert.NotNull(result.DeletedAt);
    }

    [Fact]
    public async Task DeletePostAsync_WritesAuditLog()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1);

        await _sut.DeletePostAsync(
            1,
            deletedByUserId: 99,
            ipAddress: "10.0.0.1",
            userAgent: "Browser",
            traceId: "trace-d",
            CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.PostDeletedByAdmin, _auditLog.LoggedAction);
    }

    #endregion

    #region DeletePostAsync — Validation Failures

    [Fact]
    public async Task DeletePostAsync_NonExistentPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeletePostAsync(9999, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task DeletePostAsync_AlreadyDeleted_ThrowsBusinessException409()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeletePostAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostAlreadyDeleted, ex.ResponseCode);
    }

    #endregion

    #region RestorePostAsync — Success

    [Fact]
    public async Task RestorePostAsync_DeletedPost_RestoresToDraft()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true, status: PostStatus.Deleted);

        var result = await _sut.RestorePostAsync(
            1,
            restoredByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal(1, result.Id);
        Assert.Equal("Draft", result.Status);
        Assert.False(result.IsDeleted);
    }

    [Fact]
    public async Task RestorePostAsync_WritesAuditLog()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true);

        await _sut.RestorePostAsync(
            1,
            restoredByUserId: 99,
            ipAddress: "10.0.0.1",
            userAgent: "Browser",
            traceId: "trace-r",
            CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.PostRestoredByAdmin, _auditLog.LoggedAction);
    }

    #endregion

    #region RestorePostAsync — Validation Failures

    [Fact]
    public async Task RestorePostAsync_NonExistentPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestorePostAsync(9999, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task RestorePostAsync_NotDeletedPost_ThrowsBusinessException409()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: false);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.RestorePostAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotDeleted, ex.ResponseCode);
    }

    #endregion

    #region Slug Generation

    [Fact]
    public async Task CreatePostAsync_GeneratesUniqueSlug()
    {
        var admin = await SeedAdminUserAsync(99);
        await SeedPostAsync(1, authorId: 99, slug: "existing-post");

        var request = new CreateAdminPostRequest
        {
            Title = "Existing Post",
            Content = "<p>Content</p>"
        };

        var result = await _sut.CreatePostAsync(
            request,
            createdByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.NotEqual("existing-post", result.Slug);
        Assert.StartsWith("existing-post-", result.Slug);
    }

    #endregion

    #region ChangePostStatusAsync — Success

    [Fact]
    public async Task ChangePostStatusAsync_HidePublishedPost_ChangesStatusToHidden()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);

        var request = new ChangePostStatusRequest { Status = "Hidden" };

        var result = await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-s1",
            CancellationToken.None);

        Assert.Equal("Published", result.PreviousStatus);
        Assert.Equal("Hidden", result.CurrentStatus);
        Assert.Equal(99, result.UpdatedBy);

        var post = await _db.Posts.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Equal(PostStatus.Hidden, post.Status);
    }

    [Fact]
    public async Task ChangePostStatusAsync_ShowHiddenPost_ChangesStatusToPublished()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Hidden);

        var request = new ChangePostStatusRequest { Status = "Published" };

        var result = await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Hidden", result.PreviousStatus);
        Assert.Equal("Published", result.CurrentStatus);
    }

    [Fact]
    public async Task ChangePostStatusAsync_PublishDraft_ChangesStatusToPublished()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Draft);

        var request = new ChangePostStatusRequest { Status = "Published" };

        var result = await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Draft", result.PreviousStatus);
        Assert.Equal("Published", result.CurrentStatus);
    }

    [Fact]
    public async Task ChangePostStatusAsync_PublishedToArchived_ChangesStatusToArchived()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);

        var request = new ChangePostStatusRequest { Status = "Archived" };

        var result = await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        Assert.Equal("Published", result.PreviousStatus);
        Assert.Equal("Archived", result.CurrentStatus);
    }

    [Fact]
    public async Task ChangePostStatusAsync_WritesAuditLog()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);

        var request = new ChangePostStatusRequest { Status = "Hidden" };

        await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: "10.0.0.1",
            userAgent: "Browser",
            traceId: "trace-audit",
            CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.PostVisibilityChanged, _auditLog.LoggedAction);
        Assert.Equal(nameof(Post), _auditLog.LoggedEntityType);
        Assert.Contains("Published", _auditLog.LoggedDetails ?? "");
        Assert.Contains("Hidden", _auditLog.LoggedDetails ?? "");
    }

    [Fact]
    public async Task ChangePostStatusAsync_ClearPublishedAt_WhenHidden()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        var post = await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);
        post.PublishedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var request = new ChangePostStatusRequest { Status = "Hidden" };

        await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        var dbPost = await _db.Posts.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.Null(dbPost.PublishedAt);
    }

    [Fact]
    public async Task ChangePostStatusAsync_SetPublishedAt_WhenPublished()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Draft);

        var request = new ChangePostStatusRequest { Status = "Published" };

        await _sut.ChangePostStatusAsync(
            postId: 1,
            request,
            changedByUserId: 99,
            ipAddress: null,
            userAgent: null,
            traceId: null,
            CancellationToken.None);

        var dbPost = await _db.Posts.AsNoTracking().FirstAsync(p => p.Id == 1);
        Assert.NotNull(dbPost.PublishedAt);
    }

    #endregion

    #region ChangePostStatusAsync — Validation Failures

    [Fact]
    public async Task ChangePostStatusAsync_NonExistentPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var request = new ChangePostStatusRequest { Status = "Hidden" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(9999, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangePostStatusAsync_DeletedPost_ThrowsBusinessException404()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, isDeleted: true);

        var request = new ChangePostStatusRequest { Status = "Hidden" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostNotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangePostStatusAsync_InvalidStatus_ThrowsBusinessException400()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);

        var request = new ChangePostStatusRequest { Status = "NotARealStatus" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseCodes.InvalidPostStatus, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangePostStatusAsync_AlreadyInTargetStatus_ThrowsBusinessException409()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Published);

        var request = new ChangePostStatusRequest { Status = "Published" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.PostAlreadyInTargetStatus, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangePostStatusAsync_InvalidTransition_DraftToHidden_ThrowsBusinessException409()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Draft);

        var request = new ChangePostStatusRequest { Status = "Hidden" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.InvalidPostStatusTransition, ex.ResponseCode);
    }

    [Fact]
    public async Task ChangePostStatusAsync_InvalidTransition_HiddenToArchived_ThrowsBusinessException409()
    {
        var admin = await SeedAdminUserAsync(99);
        var author = await SeedAuthorUserAsync(1);
        await SeedPostAsync(1, authorId: 1, status: PostStatus.Hidden);

        var request = new ChangePostStatusRequest { Status = "Archived" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.ChangePostStatusAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.InvalidPostStatusTransition, ex.ResponseCode);
    }

    #endregion
}
