using System;

namespace ASC.Model.BaseTypes
{
    public static class Constants
    {
        // Định nghĩa các hằng số nếu cần
    }

    public enum Roles
    {
        Admin, Engineer, User

    }

    public enum MasterKeys
    {
        VehicleName,
        VehicleType
    }

    public enum Status
    {
        New,
        Denied,
        Pending,
        Initiated,
        InProgress,
        PendingCustomerApproval,
        RequestForInformation,
        Completed
    }
}
