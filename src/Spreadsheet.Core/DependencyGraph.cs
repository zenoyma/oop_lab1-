namespace Spreadsheet.Core;

// Хто на кого посилається. Якщо B1 = inc(A1): A1 - попередник B1, а B1 - залежна від A1
public class DependencyGraph
{
    // B1 -> {A1}: на кого посилається клітинка
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> precedents = new();
    // A1 -> {B1}: хто залежить від клітинки (щоб знати, кого перерахувати)
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> dependents = new();

    // Записує нові посилання клітинки замість старих
    public void SetPrecedents(CellAddress cell, ISet<CellAddress> newPrecedents)
    {
        Remove(cell);
        if (newPrecedents.Count == 0)
        {
            return;
        }
        precedents[cell] = new HashSet<CellAddress>(newPrecedents);
        foreach (var p in newPrecedents)
        {
            if (!dependents.TryGetValue(p, out var set))
            {
                dependents[p] = set = new HashSet<CellAddress>();
            }
            set.Add(cell);
        }
    }

    // Прибирає посилання самої клітинки; ті, хто посилається на неї, лишаються
    public void Remove(CellAddress cell)
    {
        if (!precedents.Remove(cell, out var old))
        {
            return;
        }
        foreach (var p in old)
        {
            dependents[p].Remove(cell);
            if (dependents[p].Count == 0)
            {
                dependents.Remove(p);
            }
        }
    }

    // Кого і в якому порядку перерахувати після зміни клітинок changed
    public RecalcPlan GetRecalcOrder(IEnumerable<CellAddress> changed)
    {
        HashSet<CellAddress> affected = CollectAffected(changed);

        // Порядок перерахунку: клітинку рахуємо, коли всі, від кого вона залежить, уже пораховані.
        // inDegree = скільки її попередників ще не пораховано; рахуємо тих, у кого 0
        var inDegree = new Dictionary<CellAddress, int>();
        foreach (var cell in affected)
        {
            inDegree[cell] = precedents.TryGetValue(cell, out var ps) ? ps.Count(affected.Contains) : 0;
        }

        var plan = new RecalcPlan();
        var ready = new Queue<CellAddress>(affected.Where(c => inDegree[c] == 0));
        while (ready.Count > 0)
        {
            var cell = ready.Dequeue();
            plan.Order.Add(cell);
            foreach (var d in dependents.GetValueOrDefault(cell) ?? new HashSet<CellAddress>())
            {
                if (--inDegree[d] == 0)
                {
                    ready.Enqueue(d);
                }
            }
        }

        // У кого лічильник так і не став 0 - ті в циклі або залежать від нього
        plan.CycleCells.UnionWith(affected.Where(c => inDegree[c] > 0));
        return plan;
    }

    // Пошук у ширину (BFS): змінені клітинки + усі, хто від них залежить (і напряму, і через інших)
    private HashSet<CellAddress> CollectAffected(IEnumerable<CellAddress> changed)
    {
        var visited = new HashSet<CellAddress>(changed);
        var queue = new Queue<CellAddress>(visited);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var d in dependents.GetValueOrDefault(cell) ?? new HashSet<CellAddress>())
            {
                if (visited.Add(d))
                {
                    queue.Enqueue(d);
                }
            }
        }
        return visited;
    }
}
