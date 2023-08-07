using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Astrolabe.Common;

public class ListEditor
{
    public static ListEditResults<TDb> EditList<TEdit, TDb>(ICollection<TDb>? existing,
        IEnumerable<TEdit> edits, Func<TEdit, TDb, bool> matching, Func<TEdit, TDb, bool> edit, Func<TEdit, TDb> newEntity)
    {
        existing ??= new List<TDb>();
        var added = new List<TDb>();
        var edited = new List<TDb>();
        var newEntries = edits.Select(m =>
        {
            var dbEntity = existing.FirstOrDefault(tdb => matching(m, tdb));
            bool shouldAdd = false;
            if (dbEntity == null)
            {
                dbEntity = newEntity(m);
                shouldAdd = true;
            }

            var different = edit(m, dbEntity);
            if (shouldAdd) added.Add(dbEntity);
            else if (different) edited.Add(dbEntity);
            return dbEntity;
        }).ToList();
        var removed = existing.Where(dm => !newEntries.Contains(dm)).ToList();
        return new ListEditResults<TDb>(newEntries, added, removed, edited);
    }

    public async static Task<ListEditResults<TDb>> EditListAsync<TEdit, TDb>(ICollection<TDb>? existing,
        IEnumerable<TEdit> edits, Func<TEdit, TDb, bool> matching, Func<TEdit, TDb, Task<bool>> edit, Func<TEdit, TDb> newEntity)
    {
        existing ??= new List<TDb>();
        var added = new List<TDb>();
        var edited = new List<TDb>();
        var newEntries = new List<TDb>();
        foreach (var m in edits)
        {
            var dbEntity = existing.FirstOrDefault(tdb => matching(m, tdb));
            var shouldAdd = false;
            if (dbEntity == null)
            {
                dbEntity = newEntity(m);
                shouldAdd = true;
            }

            var different = await edit(m, dbEntity);
            if (shouldAdd) added.Add(dbEntity);
            else if (different) edited.Add(dbEntity);
            newEntries.Add(dbEntity);
        }

        var removed = existing.Where(dm => !newEntries.Contains(dm)).ToList();
        return new ListEditResults<TDb>(newEntries, added, removed, edited);
    }
}

public class ListEditResults<T>
{
    public List<T> Added { get; }
    public List<T> Removed { get; }
    public List<T> Edited { get; }

    public List<T> Result { get; }

    public ListEditResults(List<T> result, List<T> added, List<T> removed, List<T> edited)
    {
        Result = result;
        Added = added;
        Removed = removed;
        Edited = edited;
    }

    public List<T> WithOrdering(Action<T, int> addOrder)
    {
        return Result.Select((r, i) =>
        {
            addOrder(r, i);
            return r;
        }).ToList();
    }

    public IEnumerable<T> MarkRemoved(Action<T> setDeleted)
    {
        foreach (var r in Removed)
        {
            setDeleted(r);
        }

        return Removed;
    }
}