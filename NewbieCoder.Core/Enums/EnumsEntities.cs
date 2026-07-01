namespace NewbieCoder.Core.Enums;

public enum CommunityQuestionStatus { Open = 1, Answered = 2, Resolved = 3, Closed = 4, Hidden = 5 }
public enum DeviceStatus           { Active = 1, Revoked = 2, Blocked = 3 }
public enum DeviceType             { Web = 1, Mobile = 2, Tablet = 3, Desktop = 4 }
public enum EntityStatus           { Active = 1, Inactive = 2 }
public enum InterviewLevel         { Entry = 1, Junior = 2, Middle = 3, Senior = 4, Expert = 5 }
public enum LoginStatus            { Success = 1, Failed = 2 }
public enum PostStatus             { Draft = 1, Pending = 2, Published = 3, Rejected = 4, Hidden = 5, Deleted = 6 }
public enum PostVisibility         { Public = 1, Private = 2, LinkOnly = 3 }
public enum RoleStatus             { Active = 1, Inactive = 2 }
public enum SessionStatus          { Active = 1, Expired = 2, Revoked = 3 }
public enum SortDirection          { Asc, Desc }
public enum TokenStatus            { Active = 1, Used = 2, Revoked = 3, Expired = 4 }
public enum UserRoleStatus         { Active = 1, Revoked = 2, Expired = 3 }
public enum UserStatus             { Active = 1, Inactive = 2, Banned = 3, Closed = 4 }
