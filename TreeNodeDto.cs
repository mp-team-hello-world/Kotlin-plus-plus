using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kotlin_plus_plus;

public class TreeNodeDto
{
    // Имя узла (например, "Statements" или "'val'")
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    // Список дочерних узлов (если пустой — сериализатор его не проигнорирует, но фронтенд с ним справится)
    [JsonPropertyName("children")]
    public List<TreeNodeDto> Children { get; set; } = new();
}