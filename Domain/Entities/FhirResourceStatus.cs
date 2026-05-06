namespace Domain.Entities
{
    public class FhirResourceStatus(string resourceName, Guid instituteBaseUrlId)
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string ResourceName { get; private set; } = resourceName;
        public bool IsVerified { get; private set; } = false;
        public string? ErrorMessage { get; private set; }
        public Guid InstituteBaseUrlId { get; private set; } = instituteBaseUrlId;
        public InstituteBaserUrl InstituteBaseUrl { get; private set; } = null!;

        public void MarkVerified()
        {
            IsVerified = true;
            ErrorMessage = null;
        }

        public void MarkFailed(string error)
        {
            IsVerified = false;
            ErrorMessage = error;
        }
    }
}
