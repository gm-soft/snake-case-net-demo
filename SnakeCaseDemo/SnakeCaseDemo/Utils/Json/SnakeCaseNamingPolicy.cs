using System.Text.Json;

namespace SnakeCaseDemo.Utils.Json
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToSnakeCase();
    }
}