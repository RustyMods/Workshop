using System.Collections.Generic;

namespace Workshop;

public class PieceDict : Dictionary<string, Piece.Requirement>
{
    public void Add(Piece.Requirement req)
    {
        string item = req.m_resItem.m_itemData.m_shared.m_name;
        if (TryGetValue(item, out Piece.Requirement res))
        {
            res.m_amount += req.m_amount;
        }
        else
        {
            this[item] = new Piece.Requirement
            {
                m_resItem = req.m_resItem,
                m_amount = req.m_amount,
            };
        }
    }

    public void Remove(Piece.Requirement req)
    {
        string item = req.m_resItem.m_itemData.m_shared.m_name;
        if (TryGetValue(item, out Piece.Requirement res))
        {
            res.m_amount -= req.m_amount;
            
            if (res.m_amount <= 0)
            {
                Remove(item);
            }
        }
    }
}