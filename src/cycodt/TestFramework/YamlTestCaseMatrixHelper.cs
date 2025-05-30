public static class YamlTestCaseMatrixHelpers
{
    public static List<Dictionary<string, string>> GetNewMatrix()
    {
        var matrix = new List<Dictionary<string, string>>();
        matrix.Add(GetNewMatrixItemDictionary());
        return matrix;
    }

    public static Dictionary<string, string> GetNewMatrixItemDictionary(Dictionary<string, string>? existing = null)
    {
        var matrixItem = existing != null
            ? new Dictionary<string, string>(existing)
            : new Dictionary<string, string>();
        UpdateMatrixId(matrixItem);
        return matrixItem;
    }

    public static void UpdateMatrixId(Dictionary<string, string> matrixItem)
    {
        matrixItem[matrixIdKey] = Guid.NewGuid().ToString();
    }

    public static string? GetMatrixId(Dictionary<string, string> matrixItem)
    {
        return matrixItem.ContainsKey(matrixIdKey)
            ? matrixItem[matrixIdKey]
            : null;
    }

    private const string matrixIdKey = "__matrix_id__";
}
