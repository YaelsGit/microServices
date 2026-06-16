namespace SharedModels.Constants;

public static class ApiRoutes
{
    // Auth routes
    public const string AUTH_LOGIN = "/auth/login";
    public const string AUTH_REGISTER = "/auth/register";
    public const string AUTH_VALIDATE = "/auth/validate";
    public const string AUTH_CHANGE_PASSWORD = "/auth/change-password";

    // Catalog routes - Gifts
    public const string CATALOG_GIFTS = "/catalog/gifts";
    public const string CATALOG_GIFTS_BY_ID = "/catalog/gifts/{id}";
    public const string CATALOG_GIFTS_FILTER = "/catalog/gifts/filter";

    // Catalog routes - Donors
    public const string CATALOG_DONORS = "/catalog/donors";
    public const string CATALOG_DONORS_BY_ID = "/catalog/donors/{id}";

    // Catalog routes - Categories
    public const string CATALOG_CATEGORIES = "/catalog/categories";
    public const string CATALOG_CATEGORIES_BY_ID = "/catalog/categories/{id}";

    // Order routes
    public const string ORDERS_CREATE = "/orders/create";
    public const string ORDERS_GET_BY_USER = "/orders/user/{userId}";
    public const string ORDERS_GET_BY_ID = "/orders/{id}";
    public const string ORDERS_CANCEL = "/orders/{id}/cancel";
    public const string ORDERS_REPORT_BY_GIFT = "/orders/reports/by-gift";
    public const string ORDERS_REPORT_BY_PRICE = "/orders/reports/by-price-range";

    // Lottery routes
    public const string LOTTERY_RUN = "/lottery/run";
    public const string LOTTERY_WINNERS = "/lottery/winners";
    public const string LOTTERY_TICKETS = "/lottery/tickets/{userId}";
    public const string LOTTERY_STATISTICS = "/lottery/statistics";
    public const string LOTTERY_CREATE_TICKET = "/lottery/tickets/create";
}
