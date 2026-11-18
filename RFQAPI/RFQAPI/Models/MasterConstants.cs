namespace RFQAPI.Models;

public static class UserRoles
{
    public const string Supplier = "Supplier";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string Approver = "Approver";
}

public static class SupplierApprovalStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Suspended = "Suspended";
}

public static class RfqStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Closed = "Closed";
    public const string Evaluated = "Evaluated";
    public const string Awarded = "Awarded";
}

public static class BidStatuses
{
    public const string Submitted = "Submitted";
    public const string Withdrawn = "Withdrawn";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
}

public static class RoundStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Closed = "Closed";
    public const string Evaluated = "Evaluated";
    public const string Cancelled = "Cancelled";
}

public static class AuditActions
{
    public const string SupplierStatusUpdated = "SupplierStatusUpdated";
    public const string RfqCreated = "RfqCreated";
    public const string RfqUpdated = "RfqUpdated";
    public const string RfqRoundCreated = "RfqRoundCreated";
    public const string RfqRoundPublished = "RfqRoundPublished";
    public const string RfqRoundClosed = "RfqRoundClosed";
    public const string BidSubmitted = "BidSubmitted";
    public const string BidWithdrawn = "BidWithdrawn";
    public const string RfqRoundEvaluated = "RfqRoundEvaluated";
    public const string EvaluationApproved = "EvaluationApproved";
    public const string EvaluationRejected = "EvaluationRejected";
    public const string RfqAwarded = "RfqAwarded";
}

public static class EvaluationDecisions
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class EvaluationDecisionStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class AuditEntityTypes
{
    public const string Supplier = "Supplier";
    public const string Rfq = "Rfq";
    public const string RfqRound = "RfqRound";
    public const string Bid = "Bid";
}