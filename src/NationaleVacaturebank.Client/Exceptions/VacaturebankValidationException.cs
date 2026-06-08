namespace NationaleVacaturebank.Client.Exceptions;

public sealed class VacaturebankValidationException : Exception
{
    public string ParameterName { get; }

    public VacaturebankValidationException(string parameterName, string message)
        : base(message)
    {
        ParameterName = parameterName;
    }
}
