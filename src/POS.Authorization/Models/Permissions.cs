namespace POS.Authorization.Models;

public static class Permissions
{
    public static readonly Dictionary<string, string> RolePermissions = new()
    {
        // ── Nav / Screens ─────────────────────────────────────────────────────
        { "CanAccessTables",       "CanAccessTables" },
        { "CanAccessDelivery",     "CanAccessDelivery" },
        { "CanAccessTakeAway",     "CanAccessTakeAway" },
        { "CanAccessAccounts",     "CanAccessAccounts" },
        { "CanAccessSummary",      "CanAccessSummary" },
        { "CanAccessOrders",       "CanAccessOrders" },
        { "CanAccessDistribution", "CanAccessDistribution" },
        { "CanAccessWaiting",      "CanAccessWaiting" },

        // ── Section 3 – Item Actions ──────────────────────────────────────────
        { "CanUseKeypad",          "CanUseKeypad" },
        { "CanIncrementQuantity",  "CanIncrementQuantity" },
        { "CanDecrementQuantity",  "CanDecrementQuantity" },
        { "CanDeleteItem",         "CanDeleteItem" },
        { "CanApplyItemDiscount",  "CanApplyItemDiscount" },
        { "CanEditItemComment",    "CanEditItemComment" },

        // ── Section 4 – Order Actions ─────────────────────────────────────────
        { "CanPrintOrder",    "CanPrintOrder" },
        { "CanWaitingOrder",  "CanWaitingOrder" },
        { "CanCancelOrder",   "CanCancelOrder" },

        // ── Footer Buttons ────────────────────────────────────────────────────
        { "CanAccessFooterDiscountBtn",      "CanAccessFooterDiscountBtn" },
        { "CanAccessFooterCustomerDataBtn",  "CanAccessFooterCustomerDataBtn" },
        { "CanAccessFooterPaymentMethodBtn", "CanAccessFooterPaymentMethodBtn" },
        { "CanAccessFooterQuickPaymentBtn",  "CanAccessFooterQuickPaymentBtn" },
        { "CanAccessFooterMealsBtn",         "CanAccessFooterMealsBtn" },
        { "CanAccessFooterHospitalityBtn",   "CanAccessFooterHospitalityBtn" },
        { "CanAccessFooterWaitingBtn",       "CanAccessFooterWaitingBtn" },
        { "CanAccessFooterSettingsBtn",      "CanAccessFooterSettingsBtn" },

        // ── DineIn Buttons ────────────────────────────────────────────────────
        { "CanAccessDineInOrderBtn",       "CanAccessDineInOrderBtn" },
        { "CanAccessDineInReceiptBtn",     "CanAccessDineInReceiptBtn" },
        { "CanAccessDineInCloseTableBtn",  "CanAccessDineInCloseTableBtn" },
        { "CanAccessDineInSplitOrderBtn",  "CanAccessDineInSplitOrderBtn" },
        { "CanAccessDineInMergeTableBtn",  "CanAccessDineInMergeTableBtn" },
        { "CanAccessDineInTransferBtn",    "CanAccessDineInTransferBtn" },
        { "CanAccessDineInVoidBtn",        "CanAccessDineInVoidBtn" },
        { "CanAccessDineInGuestCountBtn",  "CanAccessDineInGuestCountBtn" },

        // ── Delivery Buttons ──────────────────────────────────────────────────
        { "CanAccessDeliveryOrderBtn",              "CanAccessDeliveryOrderBtn" },
        { "CanAccessDeliveryAddNewBtn",             "CanAccessDeliveryAddNewBtn" },
        { "CanAccessDeliveryClearBtn",              "CanAccessDeliveryClearBtn" },
        { "CanAccessDeliveryComplaintsBtn",         "CanAccessDeliveryComplaintsBtn" },
        { "CanAccessDeliverySearchBtn",             "CanAccessDeliverySearchBtn" },
        { "CanAccessDeliveryBranchManagementBtn",   "CanAccessDeliveryBranchManagementBtn" },
        { "CanAccessDeliveryDistributionBtn",       "CanAccessDeliveryDistributionBtn" },
        { "CanAccessDeliveryToggleDirectionBtn",    "CanAccessDeliveryToggleDirectionBtn" },
        { "CanAccessDeliveryVoidBtn",               "CanAccessDeliveryVoidBtn" },
        { "CanAccessDeliveryEditBtn",               "CanAccessDeliveryEditBtn" },

        // ── All Orders (Today) Actions ────────────────────────────────────────
        { "CanViewOrderDetails",          "CanViewOrderDetails" },
        { "CanPrintOrderCustomerReceipt", "CanPrintOrderCustomerReceipt" },
        { "CanPrintOrderKitchenReceipt",  "CanPrintOrderKitchenReceipt" },
        { "CanVoidOrderFromList",         "CanVoidOrderFromList" },

        // ── Distribution Buttons ──────────────────────────────────────────────
        { "CanAccessDistributionAssignBtn",           "CanAccessDistributionAssignBtn" },
        { "CanAccessDistributionViewBtn",             "CanAccessDistributionViewBtn" },
        { "CanAccessDistributionVoidBtn",             "CanAccessDistributionVoidBtn" },
        { "CanAccessDistributionPrintBtn",            "CanAccessDistributionPrintBtn" },
        { "CanAccessDistributionUnDispatchBtn",       "CanAccessDistributionUnDispatchBtn" },
        { "CanAccessDistributionCollectBtn",          "CanAccessDistributionCollectBtn" },
        { "CanAccessDistributionVoidHistoryBtn",      "CanAccessDistributionVoidHistoryBtn" },
        { "CanAccessDistributionDriverSettlementBtn", "CanAccessDistributionDriverSettlementBtn" },
        { "CanAccessDistributionViewDriversBtn",      "CanAccessDistributionViewDriversBtn" },

        // ── Waiting Page Actions ──────────────────────────────────────────────
        { "CanCompleteWaitingOrder", "CanCompleteWaitingOrder" },
        { "CanRemoveWaitingOrder",   "CanRemoveWaitingOrder" },

        // ── Summary Actions ───────────────────────────────────────────────────
        { "CanViewSummaryDetails", "CanViewSummaryDetails" },
        { "CanViewSalesItems", "CanViewSalesItems" },
        { "CanPrintSalesItems", "CanPrintSalesItems" },
        { "CanViewDetailedSales", "CanViewDetailedSales" },
        { "CanPrintSummaryReport", "CanPrintSummaryReport" },

        // ── Accounts Actions ──────────────────────────────────────────────────
        { "CanViewStaffAccounts",  "CanViewStaffAccounts" },
        { "CanPrintStaffAccounts", "CanPrintStaffAccounts" },

        // ── Global ────────────────────────────────────────────────────────────
        { "CanAccessPosSettingsFeature", "CanAccessPosSettingsFeature" },

        // ── BackOffice Screens ───────────────────────────────────────────────
        { "CanViewDashboardAtBackOffice", "CanViewDashboardAtBackOffice" },
        { "CanManageCompanySettingsAtBackOffice", "CanManageCompanySettingsAtBackOffice" },
        { "CanManageOrderSettingsAtBackOffice", "CanManageOrderSettingsAtBackOffice" },
        { "CanManagePrinterSettingsAtBackOffice", "CanManagePrinterSettingsAtBackOffice" },
        { "CanManageSystemFeaturesAtBackOffice", "CanManageSystemFeaturesAtBackOffice" },
        { "CanSyncDataAtBackOffice", "CanSyncDataAtBackOffice" },
        { "CanManageGeneralSettingsAtBackOffice", "CanManageGeneralSettingsAtBackOffice" },
        { "CanManagePosSettingsAtBackOffice", "CanManagePosSettingsAtBackOffice" },
        { "CanManageLanguageSettingsAtBackOffice", "CanManageLanguageSettingsAtBackOffice" },
        { "CanManageUsersAtBackOffice", "CanManageUsersAtBackOffice" },
        { "CanResetUserPasswordAtBackOffice", "CanResetUserPasswordAtBackOffice" },
        { "CanChangeOwnPasswordAtBackOffice", "CanChangeOwnPasswordAtBackOffice" },
        { "CanManageRolesAtBackOffice", "CanManageRolesAtBackOffice" },
        { "CanManagePermissionsAtBackOffice", "CanManagePermissionsAtBackOffice" },
        { "CanManageItemsAtBackOffice", "CanManageItemsAtBackOffice" },
        { "CanManageZonesAtBackOffice", "CanManageZonesAtBackOffice" },
        { "CanManageTablesAtBackOffice", "CanManageTablesAtBackOffice" },
        { "CanManagePaymentMethodsAtBackOffice", "CanManagePaymentMethodsAtBackOffice" },
        { "CanManageInventoryAtBackOffice", "CanManageInventoryAtBackOffice" },
        { "CanManageRawMaterialsAtBackOffice", "CanManageRawMaterialsAtBackOffice" },
        { "CanManageSemiFinishedItemsAtBackOffice", "CanManageSemiFinishedItemsAtBackOffice" },
        { "CanManageRecipesAtBackOffice", "CanManageRecipesAtBackOffice" },
        { "CanManageUnitsAtBackOffice", "CanManageUnitsAtBackOffice" },
        { "CanViewBackOfficeTransactionsAtBackOffice", "CanViewBackOfficeTransactionsAtBackOffice" },
        { "CanViewBackOfficeReportsAtBackOffice", "CanViewBackOfficeReportsAtBackOffice" },
        { "CanViewQueriesAtBackOffice", "CanViewQueriesAtBackOffice" },
        { "CanViewRegistrationAtBackOffice", "CanViewRegistrationAtBackOffice" },
        { "CanViewClosingAtBackOffice", "CanViewClosingAtBackOffice" },
        { "CanAssignDriverAtBackOffice", "CanAssignDriverAtBackOffice" },
        { "CanViewOrderAtBackOffice", "CanViewOrderAtBackOffice" },
        { "CanVoidOrderAtBackOffice", "CanVoidOrderAtBackOffice" },
        { "CanPrintOrderAtBackOffice", "CanPrintOrderAtBackOffice" },
        { "CanUnDispatchOrderAtBackOffice", "CanUnDispatchOrderAtBackOffice" },
        { "CanCollectOrderAtBackOffice", "CanCollectOrderAtBackOffice" },
        { "CanViewDriversListAtBackOffice", "CanViewDriversListAtBackOffice" },
        { "CanViewVoidHistoryAtBackOffice", "CanViewVoidHistoryAtBackOffice" },
        { "CanViewDriverSettlementAtBackOffice", "CanViewDriverSettlementAtBackOffice" },
        { "CanPrintKitchenOrderAtBackOffice", "CanPrintKitchenOrderAtBackOffice" },
        { "CanPrintStaffAccountsAtBackOffice", "CanPrintStaffAccountsAtBackOffice" },
        { "CanViewSummaryDetailsAtBackOffice", "CanViewSummaryDetailsAtBackOffice" },
        { "CanPrintSummaryReportAtBackOffice", "CanPrintSummaryReportAtBackOffice" },
        { "CanViewSalesItemsAtBackOffice", "CanViewSalesItemsAtBackOffice" },
        { "CanPrintSalesItemsAtBackOffice", "CanPrintSalesItemsAtBackOffice" },
        { "CanViewStaffMealsReportAtBackOffice", "CanViewStaffMealsReportAtBackOffice" },
        { "CanManageStaffMealConfigsAtBackOffice", "CanManageStaffMealConfigsAtBackOffice" },
        { "CanViewHospitalityReportAtBackOffice", "CanViewHospitalityReportAtBackOffice" },

        // ── Branch Delivery Orders Page ───────────────────────────────────────
        { "CanViewBranchDeliveryOrderDetails", "CanViewBranchDeliveryOrderDetails" },
        { "CanPrintBranchCustomerReceipt",     "CanPrintBranchCustomerReceipt" },
        { "CanPrintBranchDeliveryReceipt",     "CanPrintBranchDeliveryReceipt" },
        { "CanVoidBranchDeliveryOrder",        "CanVoidBranchDeliveryOrder" },
    };
}
