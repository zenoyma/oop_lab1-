namespace Spreadsheet.Core;

public enum CellError
{
    Syntax,  // вираз записано неправильно
    Ref,     // посилання на клітинку поза таблицею
    Name,    // невідоме ім'я
    Value,   // не той тип, наприклад inc(1 < 2)
    Cycle,   // клітинки посилаються одна на одну
    Loop,    // while ніяк не закінчується
}

// Варіанти помилок
public static class CellErrorText
{
    public static string Of(CellError error) => error switch
    {
        CellError.Syntax => "#SYNTAX!",
        CellError.Ref => "#REF!",
        CellError.Name => "#NAME?",
        CellError.Value => "#VALUE!",
        CellError.Cycle => "#CYCLE!",
        CellError.Loop => "#LOOP!",
        _ => "#ERROR!",
    };
}
