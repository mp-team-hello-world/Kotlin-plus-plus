using System;

namespace Kotlin_plus_plus;

public static class TranslationFactory
{
    public static ITranslationStrategy GetStrategy(string fromLanguage, string toLanguage)
    {
        var key = $"{fromLanguage.ToLower()}->{toLanguage.ToLower()}";
        
        return key switch
        {
            "kotlin->cpp" => new KotlinToCppStrategy(),
            
            _ => throw new NotSupportedException($"Перевод из {fromLanguage} в {toLanguage} пока не поддерживается.")
        };
    }
}