﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogBoxQuests : DialogBox
{
    public Transform QuestItemListPanel;
    public GameObject QuestItemPrefab;

    public override void ShowDialog()
    {
        base.ShowDialog();

        ClearInterface();
        BuildInterface();
    }

    private void ClearInterface()
    {
        List<Transform> childrens = QuestItemListPanel.Cast<Transform>().ToList();
        foreach (Transform child in childrens)
        {
            Destroy(child.gameObject);
        }
    }

    private void BuildInterface()
    {
        List<Quest> quests = PrototypeManager.Quest.Values.Where(q => IsQuestAvailable(q)).ToList();

        foreach (Quest quest in quests)
        {
            GameObject go = (GameObject)Instantiate(QuestItemPrefab);
            go.transform.SetParent(QuestItemListPanel);

            DialogBoxQuestItem questItemBehaviour = go.GetComponent<DialogBoxQuestItem>();
            questItemBehaviour.SetupQuest(this, quest);
        }
    }

    private bool IsQuestAvailable(Quest quest)
    {
        if (quest.IsAccepted)
        {
            return false;
        }

        if (quest.PreRequiredCompletedQuest.Count == 0)
        {
            return true;
        }

        List<Quest> preQuests = PrototypeManager.Quest.Values.Where(q => quest.PreRequiredCompletedQuest.Contains(q.Name)).ToList();

        return preQuests.All(q => q.IsCompleted);
    }
}