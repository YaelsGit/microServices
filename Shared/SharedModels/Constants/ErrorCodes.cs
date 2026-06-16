namespace SharedModels.Constants;

/// <summary>
/// Standard error codes used across all microservices
/// </summary>
public static class ErrorCodes
{
    // Authentication & Authorization Errors
    public const string INVALID_CREDENTIALS = "AUTH_001";
    public const string USER_NOT_FOUND = "AUTH_002";
    public const string INVALID_TOKEN = "AUTH_003";
    public const string TOKEN_EXPIRED = "AUTH_004";
    public const string UNAUTHORIZED_ACCESS = "AUTH_005";
    public const string EMAIL_ALREADY_EXISTS = "AUTH_006";
    public const string INVALID_EMAIL = "AUTH_007";
    public const string WEAK_PASSWORD = "AUTH_008";
    public const string PASSWORD_CHANGE_FAILED = "AUTH_009";

    // User Errors
    public const string USER_CREATION_FAILED = "USER_001";
    public const string USER_UPDATE_FAILED = "USER_002";
    public const string USER_DELETE_FAILED = "USER_003";
    public const string INVALID_USER_DATA = "USER_004";
    public const string USER_NOT_ACTIVE = "USER_005";

    // Catalog Errors (Gifts, Donors, Categories)
    public const string GIFT_NOT_FOUND = "CATALOG_001";
    public const string GIFT_OUT_OF_STOCK = "CATALOG_002";
    public const string INVALID_GIFT_DATA = "CATALOG_003";
    public const string GIFT_CREATION_FAILED = "CATALOG_004";
    public const string GIFT_UPDATE_FAILED = "CATALOG_005";
    public const string GIFT_DELETE_FAILED = "CATALOG_006";

    public const string DONOR_NOT_FOUND = "CATALOG_101";
    public const string DONOR_CREATION_FAILED = "CATALOG_102";
    public const string DONOR_UPDATE_FAILED = "CATALOG_103";
    public const string DONOR_DELETE_FAILED = "CATALOG_104";
    public const string INVALID_DONOR_DATA = "CATALOG_105";

    public const string CATEGORY_NOT_FOUND = "CATALOG_201";
    public const string CATEGORY_CREATION_FAILED = "CATALOG_202";
    public const string CATEGORY_UPDATE_FAILED = "CATALOG_203";
    public const string CATEGORY_DELETE_FAILED = "CATALOG_204";
    public const string INVALID_CATEGORY_DATA = "CATALOG_205";

    // Order Errors
    public const string ORDER_NOT_FOUND = "ORDER_001";
    public const string ORDER_CREATION_FAILED = "ORDER_002";
    public const string ORDER_UPDATE_FAILED = "ORDER_003";
    public const string ORDER_CANCELLATION_FAILED = "ORDER_004";
    public const string INVALID_ORDER_DATA = "ORDER_005";
    public const string INSUFFICIENT_STOCK = "ORDER_006";

    // Lottery Errors
    public const string LOTTERY_NOT_FOUND = "LOTTERY_001";
    public const string LOTTERY_CREATION_FAILED = "LOTTERY_002";
    public const string LOTTERY_DRAW_FAILED = "LOTTERY_003";
    public const string INVALID_LOTTERY_DATA = "LOTTERY_004";
    public const string LOTTERY_NO_TICKETS = "LOTTERY_005";
    public const string DUPLICATE_LOTTERY_ENTRY = "LOTTERY_006";

    // Database Errors
    public const string DATABASE_ERROR = "DB_001";
    public const string CONFLICT_ERROR = "DB_002";
    public const string VALIDATION_ERROR = "DB_003";

    // Service Communication Errors
    public const string SERVICE_UNAVAILABLE = "SERVICE_001";
    public const string SERVICE_TIMEOUT = "SERVICE_002";
    public const string SERVICE_ERROR = "SERVICE_003";

    // General Errors
    public const string INTERNAL_SERVER_ERROR = "GENERAL_001";
    public const string BAD_REQUEST = "GENERAL_002";
    public const string NOT_FOUND = "GENERAL_003";
    public const string CONFLICT = "GENERAL_004";
    public const string UNPROCESSABLE_ENTITY = "GENERAL_005";

    /// <summary>
    /// Get human-readable error message for error code
    /// </summary>
    public static string GetErrorMessage(string errorCode) => errorCode switch
    {
        INVALID_CREDENTIALS => "Invalid email or password",
        USER_NOT_FOUND => "User not found",
        INVALID_TOKEN => "Invalid authentication token",
        TOKEN_EXPIRED => "Authentication token has expired",
        UNAUTHORIZED_ACCESS => "You do not have permission to access this resource",
        EMAIL_ALREADY_EXISTS => "Email already registered",
        INVALID_EMAIL => "Invalid email format",
        WEAK_PASSWORD => "Password does not meet requirements",

        GIFT_NOT_FOUND => "Gift not found",
        GIFT_OUT_OF_STOCK => "Gift is out of stock",
        INVALID_GIFT_DATA => "Invalid gift data",

        DONOR_NOT_FOUND => "Donor not found",
        INVALID_DONOR_DATA => "Invalid donor data",

        CATEGORY_NOT_FOUND => "Category not found",
        INVALID_CATEGORY_DATA => "Invalid category data",

        ORDER_NOT_FOUND => "Order not found",
        ORDER_CREATION_FAILED => "Failed to create order",
        INSUFFICIENT_STOCK => "Insufficient stock for this order",
        INVALID_ORDER_DATA => "Invalid order data",

        LOTTERY_NOT_FOUND => "Lottery entry not found",
        LOTTERY_CREATION_FAILED => "Failed to create lottery entry",
        DUPLICATE_LOTTERY_ENTRY => "You have already entered this lottery",
        INVALID_LOTTERY_DATA => "Invalid lottery data",

        DATABASE_ERROR => "A database error occurred",
        SERVICE_UNAVAILABLE => "Service is temporarily unavailable",
        SERVICE_TIMEOUT => "Service request timed out",

        INTERNAL_SERVER_ERROR => "An internal server error occurred",
        BAD_REQUEST => "Bad request",
        NOT_FOUND => "Resource not found",
        CONFLICT => "Resource conflict",

        _ => "An unknown error occurred"
    };
}
