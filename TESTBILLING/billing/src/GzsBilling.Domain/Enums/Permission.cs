namespace GzsBilling.Domain.Enums;

[Flags]
public enum Permission : long
{
    None = 0,

    // Dashboard
    DashboardView = 1L << 0,

    // Transactions
    TransactionsView = 1L << 1,
    TransactionsCreate = 1L << 2,
    TransactionsEdit = 1L << 3,
    TransactionsDelete = 1L << 4,
    TransactionsExport = 1L << 5,

    // Payments
    PaymentsView = 1L << 6,
    PaymentsCreate = 1L << 7,
    PaymentsProcess = 1L << 8,

    // Refunds
    RefundsView = 1L << 9,
    RefundsCreate = 1L << 10,
    RefundsApprove = 1L << 11,

    // Reconciliation (Sverka)
    ReconciliationView = 1L << 12,
    ReconciliationRun = 1L << 13,

    // Reports
    ReportsView = 1L << 14,
    ReportsExport = 1L << 15,

    // Gas Stations (Zapravka)
    StationsView = 1L << 16,
    StationsCreate = 1L << 17,
    StationsEdit = 1L << 18,
    StationsDelete = 1L << 19,

    // Columns (Kalonka)
    ColumnsCreate = 1L << 20,
    ColumnsEdit = 1L << 21,
    ColumnsDelete = 1L << 22,
    ColumnsView = 1L << 23,

    // Users
    UsersView = 1L << 24,
    UsersCreate = 1L << 25,
    UsersEdit = 1L << 26,
    UsersDeactivate = 1L << 27,

    // Shareholders (Ulishdorlar)
    ShareholdersView = 1L << 28,
    ShareholdersCreate = 1L << 29,
    ShareholdersEdit = 1L << 30,
    ShareholdersDelete = 1L << 31,

    // Disputes
    DisputesView = 1L << 32,
    DisputesCreate = 1L << 33,
    DisputesResolve = 1L << 34,

    // System
    SystemSettings = 1L << 35,

    // Composite permissions for roles
    SuperAdminAll = long.MaxValue,

    AdminAll = DashboardView | TransactionsView | TransactionsCreate | TransactionsEdit |
               TransactionsExport | PaymentsView | PaymentsCreate | PaymentsProcess |
               RefundsView | RefundsCreate | RefundsApprove |
               ReconciliationView | ReconciliationRun | ReportsView | ReportsExport |
               StationsView | StationsCreate | StationsEdit | StationsDelete |
               ColumnsView | ColumnsCreate | ColumnsEdit | ColumnsDelete |
               UsersView | UsersCreate | UsersEdit | UsersDeactivate |
               ShareholdersView | ShareholdersCreate | ShareholdersEdit | ShareholdersDelete |
               DisputesView | DisputesCreate | DisputesResolve | SystemSettings,

    ManagerPermissions = DashboardView | TransactionsView | TransactionsExport |
                        PaymentsView | PaymentsProcess |
                        RefundsView | RefundsCreate | RefundsApprove |
                        ReconciliationView | ReportsView | ReportsExport |
                        StationsView | StationsCreate | StationsEdit |
                        ColumnsView | ColumnsCreate | ColumnsEdit | ColumnsDelete |
                        UsersView | UsersCreate | UsersEdit | UsersDeactivate |
                        ShareholdersView |
                        DisputesView | DisputesCreate | DisputesResolve,

    OperatorPermissions = DashboardView | TransactionsView |
                         PaymentsView | PaymentsCreate |
                         StationsView |
                         ColumnsView | ColumnsCreate | ColumnsEdit |
                         RefundsView |
                         DisputesView | DisputesCreate,

    ShareholderPermissions = DashboardView | ReportsView | ReportsExport |
                            ReconciliationView | TransactionsView | TransactionsExport |
                            StationsView | ShareholdersView |
                            PaymentsView | RefundsView | DisputesView,

    ReadOnlyPermissions = DashboardView | TransactionsView | ReportsView | ReconciliationView |
                         StationsView | ColumnsView | ShareholdersView | UsersView |
                         PaymentsView | RefundsView | DisputesView
}
