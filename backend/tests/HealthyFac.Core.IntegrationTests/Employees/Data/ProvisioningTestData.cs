namespace HealthyFac.Core.IntegrationTests.Employees.Data;

public static class ProvisioningTestData
{
    public const string CompleteEmail = "provisioning-complete@plant.com";
    public const string CompleteTwiceEmail = "provisioning-complete-twice@plant.com";
    public const string CompleteOtherIdentityEmail = "provisioning-complete-other@plant.com";
    public const string CompleteDeactivatedEmail = "provisioning-complete-deactivated@plant.com";
    public const string CompleteDeactivatedTwiceEmail = "provisioning-complete-deactivated-twice@plant.com";
    public const string FailDeactivatedEmail = "provisioning-fail-deactivated@plant.com";
    public const string FailEmail = "provisioning-fail@plant.com";
    public const string FailTwiceEmail = "provisioning-fail-twice@plant.com";
    public const string RetryFailedEmail = "provisioning-retry-failed@plant.com";
    public const string RetryProvisioningEmail = "provisioning-retry-provisioning@plant.com";
    public const string RetryActiveEmail = "provisioning-retry-active@plant.com";
    public const string DeactivateEmail = "provisioning-deactivate@plant.com";
    public const string DeactivateProvisioningEmail = "provisioning-deactivate-provisioning@plant.com";
    public const string DeactivateSelfEmail = "provisioning-deactivate-self@plant.com";
    public const string DeactivateOtherCompanyEmail = "provisioning-deactivate-other@plant.com";
    public const string DeactivateTwiceEmail = "provisioning-deactivate-twice@plant.com";
    public const string RetryOtherCompanyEmail = "provisioning-retry-other@plant.com";
    public const string RetryRaceEmail = "provisioning-retry-race@plant.com";
    public const string FailActiveEmail = "provisioning-fail-active@plant.com";

    public const string FailureReason = "OrganizationNotFound";
}
