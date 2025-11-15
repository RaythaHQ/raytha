using Raytha.Application.Common.Exceptions;
using Raytha.Domain.Common;

public class ImportMethod : ValueObject
{
    public const string UPDATE_EXISTING_RECORDS_ONLY = "update_existing_records_only";
    public const string UPSERT_ALL_RECORDS = "upsert_all_records";
    public const string ADD_NEW_RECORDS_ONLY = "add_new_records_only";

    static ImportMethod() { }

    public ImportMethod() { }

    private ImportMethod(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static ImportMethod From(string developerName)
    {
        var type = SupportedImportMethods.FirstOrDefault(p =>
            p.DeveloperName == developerName.ToLower()
        );

        if (type == null)
        {
            throw new NotFoundException(developerName);
        }

        return type;
    }

    public static ImportMethod UpdateExistingRecordsOnly =>
        new("Update existing records only", UPDATE_EXISTING_RECORDS_ONLY);
    public static ImportMethod UpsertAllRecords => new("Upsert all records", UPSERT_ALL_RECORDS);
    public static ImportMethod AddNewRecordsOnly =>
        new("Add new records only", ADD_NEW_RECORDS_ONLY);

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(ImportMethod scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator ImportMethod(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<ImportMethod> SupportedImportMethods
    {
        get
        {
            yield return UpdateExistingRecordsOnly;
            yield return UpsertAllRecords;
            yield return AddNewRecordsOnly;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
