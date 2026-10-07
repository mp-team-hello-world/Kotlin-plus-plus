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

    // Оригинальный кусок Kotlin-кода, который покрывает этот узел
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}