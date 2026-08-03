namespace NewbieCoder.Core.Constants;

public static class ResponseMessages
{
    public const string Success = "Success";
    public const string InternalError = "An unexpected error occurred.";
    public const string TooManyRequests = "Too many requests. Please try again later.";

    // Authentication
    public const string InvalidCredentials = "Invalid credentials.";
    public const string UserBlocked = "Your account has been locked.";
    public const string UserPending = "Your account is pending activation.";
    public const string DeviceBlocked = "This device has been blocked from logging in.";
    public const string ValidationError = "Invalid login data.";
    public const string LoginIdRequired = "Please enter your email or username.";
    public const string PasswordRequired = "Please enter your password.";
    public const string TooManyLoginAttempts = "Too many failed login attempts. Please try again after 15 minutes.";
    public const string LogoutSuccess = "Logged out successfully.";
    public const string SessionNotFound = "Session not found or has expired.";
    public const string SessionRevoked = "This session has been revoked.";
    public const string Unauthenticated = "Please log in to continue.";
    public const string UserNotFound = "Account does not exist.";

    // User lockout
    public const string UserLocked           = "Your account has been locked.";
    public const string UserAlreadyLocked    = "Account is already locked.";
    public const string CannotLockSelf       = "You cannot lock your own account.";
    public const string CannotLockSuperAdmin = "Cannot lock a super admin account.";
    public const string InvalidLockUntil     = "Lock expiration time must be in the future.";
    public const string CannotUnlockUser     = "You do not have permission to unlock this account.";
    public const string UserNotLocked        = "Account is not currently locked.";
    public const string LockReasonEmpty   = "Lock reason cannot be empty or contain only whitespace.";
    public const string LockReasonTooShort = "Lock reason must be at least 5 characters.";
    public const string LockReasonTooLong  = "Lock reason must not exceed 500 characters.";
    public const string CannotLockDeleted  = "Cannot lock a deleted account.";
    public const string CannotUnlockDeleted = "Cannot unlock a deleted account.";
    public const string LockSucceeded      = "User account locked successfully.";
    public const string UnlockSucceeded    = "User account unlocked successfully.";

    // Role management
    public const string RoleCreatedSuccess        = "Role created successfully.";
    public const string RoleUpdatedSuccess        = "Role updated successfully.";
    public const string RoleDeletedSuccess        = "Role deleted successfully.";
    public const string RoleNameAlreadyExists     = "A role with this name already exists.";
    public const string RoleCreateFailed          = "Failed to create role.";
    public const string RoleUpdateFailed          = "Failed to update role.";
    public const string RoleDeleteFailed          = "Failed to delete role.";
    public const string SystemRoleCannotBeModified = "System roles cannot be modified.";
    public const string SystemRoleCannotBeDeleted = "System roles cannot be deleted.";
    public const string RoleIsAssignedToUsers     = "Role is currently assigned to one or more users and cannot be deleted.";
    public const string AdminRoleCannotBeDeactivated = "The admin role cannot be deactivated.";
    public const string RolesRetrieved           = "Roles retrieved successfully.";
    public const string RoleAssignedSuccess      = "Role assigned successfully.";
    public const string CannotAssignSuperAdmin  = "Cannot assign the SuperAdmin role.";
    public const string CannotAssignHigherRole  = "Cannot assign a role higher than your own role.";
    public const string CannotRemoveLastAdmin   = "Cannot remove the last Admin role.";
    public const string CannotRemoveLastSuperAdmin = "Cannot remove the last SuperAdmin role.";
    public const string CannotChangeOwnRole     = "Cannot change your own role.";
    public const string CannotAssignDeletedRole = "Cannot assign a deleted role.";
    public const string CannotAssignInactiveRole = "Cannot assign an inactive role.";
    public const string CannotAssignDeletedUser = "Cannot assign a role to a deleted user.";
    public const string CannotAssignInactiveUser = "Cannot assign a role to an inactive user.";
    public const string CannotAssignOwnRole     = "Cannot assign a role to yourself.";
    public const string CannotRevokeOwnRole   = "Cannot revoke your own role.";
    public const string RoleRevokedSuccess       = "Role revoked successfully.";
    // Password reset
    public const string InvalidEmailFormat = "Invalid email format.";
    public const string ResetTokenRequired = "Reset token is required.";
    public const string InvalidOrExpiredResetToken = "Password reset token is invalid or has expired.";
    public const string PasswordTooWeak = "Password must be at least 8 characters, including uppercase, lowercase, number and special character.";
    public const string PasswordNotMatch = "Password confirmation does not match.";
    public const string PasswordReused = "New password must not be the same as the current password.";
    public const string TooManyResetRequests = "Too many password reset requests. Please try again later.";
    public const string EmailSendFailed = "Unable to send email. Please try again later.";
    public const string ForgotPasswordSuccess = "If the email exists in our system, we have sent password reset instructions.";
    public const string ResetPasswordSuccess = "Password reset successfully. Please log in again.";
    // User management
    public const string UserUpdatedSuccess = "User account updated successfully.";
    public const string EmailAlreadyExists = "Email address is already in use.";
    public const string UsernameAlreadyExists = "Username is already taken.";
    public const string RoleNotFound = "The specified role does not exist.";
    public const string InvalidUserStatus = "Invalid user status.";
    public const string ProfileSuccess = "Profile retrieved successfully.";
    public const string SessionAlreadyRevoked = "Session has already been revoked.";
    public const string InvalidLogoutReason = "Invalid logout_reason value.";

    // Post management
    public const string PostCreatedSuccess = "Post created successfully.";
    public const string PostUpdatedSuccess = "Post updated successfully.";
    public const string PostDeletedSuccess = "Post deleted successfully.";
    public const string PostRestoredSuccess = "Post restored successfully.";
    public const string PostNotFound = "Post not found.";
    public const string PostAlreadyDeleted = "Post is already deleted.";
    public const string PostNotDeleted = "Post is not deleted.";
    public const string PostTitleRequired = "Post title is required.";
    public const string PostContentRequired = "Post content is required.";
    public const string InvalidPostStatus = "Invalid post status.";
    public const string InvalidPostStatusTransition = "The post status transition is invalid.";
    public const string AuthorNotFound = "The specified author does not exist.";
    public const string AuthorInactive = "The specified author account is inactive.";
    public const string AuthorLocked = "The specified author account is locked.";
    public const string InvalidAuthorSource = "Invalid author source configuration.";
    public const string CategoryNotFound = "The specified category does not exist.";
    public const string CategoryInactive = "The specified category is inactive.";
    public const string TagNotFound = "One or more specified tags do not exist.";
    public const string TagInactive = "One or more specified tags are inactive.";
    public const string InvalidTagIds = "One or more tag IDs are invalid.";
    public const string PostSlugAlreadyExists = "A post with this slug already exists.";
    public const string PostCreateFailed = "Failed to create post.";
    public const string PostUpdateFailed = "Failed to update post.";
    public const string PostDeleteFailed = "Failed to delete post.";
    public const string PostRestoreFailed = "Failed to restore post.";
    public const string PostConcurrencyConflict = "Post was modified by another user. Please refresh and try again.";
    public const string InvalidPostVisibility = "Invalid post visibility.";
    public const string PostAlreadyInTargetStatus = "Post is already in the target status.";
    public const string PostStatusUpdateFailed = "Failed to update post status.";

    // Interview question management
    public const string InterviewQuestionCreatedSuccess = "Interview question created successfully.";
    public const string InterviewQuestionUpdatedSuccess = "Interview question updated successfully.";
    public const string InterviewQuestionDeletedSuccess = "Interview question deleted successfully.";
    public const string InterviewQuestionRestoredSuccess = "Interview question restored successfully.";
    public const string InterviewQuestionRetrievedSuccess = "Interview question retrieved successfully.";
    public const string InterviewQuestionNotFound = "Interview question not found.";
    public const string InterviewQuestionAlreadyDeleted = "Interview question is already deleted.";
    public const string InterviewQuestionNotDeleted = "Interview question is not deleted.";
    public const string InterviewQuestionTitleRequired = "Interview question title is required.";
    public const string InterviewQuestionContentRequired = "Interview question content is required.";
    public const string InterviewQuestionAnswerRequired = "At least one answer is required when the question status is Active.";
    public const string InterviewQuestionInvalidDifficulty = "Invalid difficulty level.";
    public const string InterviewQuestionInvalidStatus = "Invalid interview question status.";
    public const string InterviewQuestionInvalidStatusTransition = "The interview question status transition is invalid.";
    public const string InterviewQuestionAlreadyInTargetStatus = "Interview question is already in the target status.";
    public const string InterviewQuestionCategoryNotFound = "The specified category does not exist.";
    public const string InterviewQuestionTagNotFound = "One or more specified tags do not exist.";
    public const string InterviewQuestionInvalidAnswers = "One or more answers are invalid.";
    public const string InterviewQuestionMultiplePreferredAnswers = "Only one answer can be marked as preferred.";
    public const string InterviewQuestionCreateFailed = "Failed to create interview question.";
    public const string InterviewQuestionUpdateFailed = "Failed to update interview question.";
    public const string InterviewQuestionDeleteFailed = "Failed to delete interview question.";
    public const string InterviewQuestionRestoreFailed = "Failed to restore interview question.";
    public const string InterviewQuestionStatusUpdateFailed = "Failed to update interview question status.";
    public const string InterviewQuestionsRetrievedSuccess = "Interview questions retrieved successfully.";

    // Category management
    public const string CategoryCreatedSuccess = "Category created successfully.";
    public const string CategoryUpdatedSuccess = "Category updated successfully.";
    public const string CategoryDeletedSuccess = "Category deleted successfully.";
    public const string CategoryRetrievedSuccess = "Category retrieved successfully.";
    public const string CategoryNameRequired = "Category name is required.";
    public const string CategorySlugRequired = "Category slug is required.";
    public const string CategorySlugAlreadyExists = "A category with this slug already exists.";
    public const string CategoryInvalidStatus = "Invalid category status.";
    public const string CategoryAlreadyInTargetStatus = "Category is already in the target status.";
    public const string CategoryStatusUpdateFailed = "Failed to update category status.";
    public const string CategoryCreateFailed = "Failed to create category.";
    public const string CategoryUpdateFailed = "Failed to update category.";
    public const string CategoryDeleteFailed = "Failed to delete category.";
    public const string CategoryHasChildren = "Cannot delete category that has child categories.";
    public const string CategoryHasPosts = "Cannot delete category that has associated posts.";

    // Tag management
    public const string TagCreatedSuccess = "Tag created successfully.";
    public const string TagUpdatedSuccess = "Tag updated successfully.";
    public const string TagDeletedSuccess = "Tag deleted successfully.";
    public const string TagMergedSuccess = "Tag merged successfully.";
    public const string TagRetrievedSuccess = "Tag retrieved successfully.";
    public const string TagNameRequired = "Tag name is required.";
    public const string TagSlugRequired = "Tag slug is required.";
    public const string TagSlugAlreadyExists = "A tag with this slug already exists.";
    public const string TagInvalidStatus = "Invalid tag status.";
    public const string TagAlreadyInTargetStatus = "Tag is already in the target status.";
    public const string TagStatusUpdateFailed = "Failed to update tag status.";
    public const string TagCreateFailed = "Failed to create tag.";
    public const string TagUpdateFailed = "Failed to update tag.";
    public const string TagDeleteFailed = "Failed to delete tag.";
    public const string TagMergeFailed = "Failed to merge tag.";
    public const string TagNotMergeableWithSelf = "Cannot merge a tag with itself.";
    public const string TagSourceNotFound = "Source tag not found.";
    public const string TagTargetNotFound = "Target tag not found.";
    public const string TagHasNoAssociations = "Tag has no associated posts or questions.";

    // Update profile
    public const string EmptyUpdateBody = "No update fields provided.";
    public const string InvalidUpdateField = "One or more fields are not allowed to be updated via this endpoint.";
    public const string UpdateProfileSuccess = "Personal information updated successfully.";
    public const string UpdateValidationFailed = "Invalid update data.";
    public const string SessionInvalid = "Invalid session.";

    // Image upload
    public const string ImageUploadSuccess = "Image uploaded successfully.";
    public const string ImageDeleteSuccess = "Image deleted successfully.";
    public const string ImageFileEmpty = "No image file provided or the file is empty.";
    public const string ImageFileTooLarge = "Image file size exceeds the maximum allowed size of {0}.";
    public const string ImageTooManyFiles = "Too many files. Maximum allowed is {0} files.";
    public const string ImageInvalidExtension = "Invalid file extension. Allowed extensions: {0}.";
    public const string ImageInvalidMimeType = "Invalid MIME type. Allowed types: {0}.";
    public const string ImageInvalidFormat = "Invalid image format or the file is corrupted.";
    public const string ImageInvalidDimensions = "Invalid image dimensions.";
    public const string ImageUnreadable = "Unable to read the image file.";
    public const string ImageWidthExceeded = "Image width ({1}) exceeds the maximum allowed width of {0} pixels.";
    public const string ImageHeightExceeded = "Image height ({1}) exceeds the maximum allowed height of {0} pixels.";
    public const string ImagePublicIdRequired = "Image public ID is required.";
    public const string ImageUploadFailed = "Failed to upload image. Please try again.";
    public const string ImageDeleteFailed = "Failed to delete image. Please try again.";
    public const string CloudinaryNotConfigured = "Image upload service is not properly configured. Please contact support.";
    // Level management
    public const string LevelCreatedSuccess = "Level created successfully.";
    public const string LevelUpdatedSuccess = "Level updated successfully.";
    public const string LevelDeletedSuccess = "Level deleted successfully.";
    public const string LevelRestoredSuccess = "Level restored successfully.";
    public const string LevelRetrievedSuccess = "Level retrieved successfully.";
    public const string LevelNotFound = "Level not found.";
    public const string LevelCodeRequired = "Level code is required.";
    public const string LevelNameRequired = "Level name is required.";
    public const string LevelCodeAlreadyExists = "A level with this code already exists.";
    public const string LevelNameAlreadyExists = "A level with this name already exists.";
    public const string LevelHasInterviewQuestions = "Cannot delete level that has associated interview questions.";

    // Community question management
    public const string CommunityQuestionRetrievedSuccess = "Community question retrieved successfully.";
    public const string CommunityQuestionDeletedSuccess = "Community question deleted successfully.";
    public const string CommunityQuestionRestoredSuccess = "Community question restored successfully.";
    public const string CommunityQuestionLockedSuccess = "Community question locked successfully.";
    public const string CommunityQuestionUnlockedSuccess = "Community question unlocked successfully.";
    public const string CommunityQuestionNotFound = "Community question not found.";
    public const string CommunityQuestionAlreadyDeleted = "Community question is already deleted.";
    public const string CommunityQuestionNotDeleted = "Community question is not deleted.";
    public const string CommunityQuestionAlreadyLocked = "Community question is already locked.";
    public const string CommunityQuestionNotLocked = "Community question is not locked.";
    public const string CommunityQuestionCannotBeModerated = "Cannot moderate a deleted community question.";
    public const string CommunityQuestionCreatedSuccess = "Community question created successfully.";
    public const string CommunityQuestionUpdatedSuccess = "Community question updated successfully.";
    public const string CommunityQuestionAuthorNotFound = "The specified author does not exist.";
    public const string CommunityQuestionTitleRequired = "Question title is required.";
    public const string CommunityQuestionContentRequired = "Question content is required.";
    public const string CommunityQuestionTitleTooShort = "Question title must be at least 10 characters.";
    public const string CommunityQuestionTitleTooLong = "Question title must not exceed 300 characters.";
    public const string CommunityQuestionContentTooShort = "Question content must be at least 20 characters.";
    public const string CommunityQuestionContentTooLong = "Question content must not exceed 10,000 characters.";
    public const string CommunityQuestionInvalidStatus = "Invalid community question status.";
    public const string CommunityQuestionInvalidStatusTransition = "The community question status transition is invalid.";
    public const string CommunityQuestionAlreadyInTargetStatus = "Community question is already in the target status.";
    public const string CommunityQuestionStatusUpdateFailed = "Failed to update community question status.";
    public const string CommunityQuestionAlreadyHidden = "Community question is already hidden.";
    public const string CommunityQuestionNotHidden = "Community question is not hidden.";
    public const string CommunityQuestionAlreadyClosed = "Community question is already closed.";
    public const string CommunityQuestionAlreadyOpen = "Community question is already open.";
    public const string CommunityQuestionResolvedRequiresAcceptedAnswer = "Cannot mark question as Resolved without an accepted answer.";
    public const string CommunityQuestionAcceptedAnswerNotFound = "Accepted answer does not exist or does not belong to this question.";
    public const string CommunityQuestionAcceptedAnswerHidden = "Accepted answer is hidden. Unhide it before marking as Resolved.";
    public const string CommunityQuestionAnsweredRequiresAnswers = "Cannot mark question as Answered without at least one valid answer.";
    public const string CommunityQuestionReasonRequired = "Reason is required for this operation.";
    public const string CommunityQuestionUpdateFailed = "Failed to update community question.";

    // Community answer management
    public const string CommunityAnswerHiddenSuccess = "Community answer hidden successfully.";
    public const string CommunityAnswerShownSuccess = "Community answer shown successfully.";
    public const string CommunityAnswerDeletedSuccess = "Community answer deleted successfully.";
    public const string CommunityAnswerRestoredSuccess = "Community answer restored successfully.";
    public const string CommunityAnswerCreatedSuccess = "Community answer created successfully.";
    public const string CommunityAnswerUpdatedSuccess = "Community answer updated successfully.";
    public const string CommunityAnswerNotFound = "Community answer not found.";
    public const string CommunityAnswerAlreadyHidden = "Community answer is already hidden.";
    public const string CommunityAnswerNotHidden = "Community answer is not hidden.";
    public const string CommunityAnswerAlreadyDeleted = "Community answer is already deleted.";
    public const string CommunityAnswerNotDeleted = "Community answer is not deleted.";
    public const string CommunityAnswerCannotBeModerated = "Cannot moderate a deleted community answer.";
    public const string CommunityAnswerQuestionNotFound = "The specified question does not exist.";
    public const string CommunityAnswerAuthorNotFound = "The specified author does not exist.";
    public const string CommunityAnswerContentRequired = "Answer content is required.";
}
