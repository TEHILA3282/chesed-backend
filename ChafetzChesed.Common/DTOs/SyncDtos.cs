namespace ChafetzChesed.Common;


public record DepositDto(
    int ID,
    int InstitutionId,
    string ClientID,
    DateTime DepositDate,
    int DepositTypeID,
    decimal Amount,
    string? PurposeDetails,
    bool IsDirectDeposit,
    DateTime? DepositReceivedDate,
    string? PaymentMethod
);


public record LoanDto(
    int ID,
    int InstitutionId,
    string ClientID,
    decimal Amount,
    int InstallmentsCount,
    DateTime LoanDate,
    string? Purpose,
    string? PurposeDetails,
    bool IsDeleted,
    DateTime UpdatedAt,
    int LoanTypeID
);

public record AuditLogDto(
    int ID,
    int InstitutionId,
    string Entity,
    string EntityId,      
    DateTime ChangedAt,
    string ChangedBy,    
    string ChangesJson
);


public record RegistrationSyncDto(
    string ID,
    int InstitutionId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string RegistrationStatus
);


public record BankAccountDto(
    int Id,
    int InstitutionId,
    string RegistrationId,
    string? BankNumber,      
    string? BranchNumber,   
    string? AccountNumber,   
    string? AccountOwnerName,
    bool HasDirectDebit
);

public record DepositWithdrawRequestDto(
    int Id,
    int InstitutionId,
    string ClientID,
    decimal Amount,
    DateTime Date,      
    string? Status        
);

public record FreezeRequestDto(
    int Id,
    int InstitutionId,
    string ClientID,
    string RequestType,   
    string Reason,
    bool Acknowledged,
    DateTime CreatedAt
);

public record ContactRequestSyncDto(
    int Id,
    int InstitutionId,
    string FirstName,
    string LastName,
    string Email,
    string Subject,
    string Message,
    DateTime CreatedAt
);

public record LoanGuarantorSyncDto(
    int Id,
    int InstitutionId,
    int LoanId,
    string IdNumber,
    string FullName,
    string Phone
);



