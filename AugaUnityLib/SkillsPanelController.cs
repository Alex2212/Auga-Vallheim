using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AugaUnity
{
    public class SkillsPanelController : MonoBehaviour
    {
        public GameObject SkillsContainer;
        public SkillsPanelSkillController SkillPrefab;

        protected readonly Dictionary<Skills.SkillType, SkillsPanelSkillController> _skills = new Dictionary<Skills.SkillType, SkillsPanelSkillController>();
        protected int _skillsCount;

        public virtual void Start()
        {
            SkillPrefab.gameObject.SetActive(false);
            Update();
        }

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                UpdateSkills(player);
            }
        }

        public virtual void UpdateSkills(Player player)
        {
            var skills = player.GetSkills();

            foreach (var skillDef in skills.m_skills)
            {
                _skills.TryGetValue(skillDef.m_skill, out var currentSkillElement);

                if (currentSkillElement == null)
                {
                    var element = Instantiate(SkillPrefab, SkillsContainer.transform, false);
                    element.SkillType = skillDef.m_skill;
                    _skills.Add(skillDef.m_skill, element);
                    element.SetActive(true);
                }
                else currentSkillElement.SetActive(true);
            }

            if (_skillsCount != _skills.Count)
            {
                _skillsCount = _skills.Count;
                SortSkillElements();
            }
        }

        public virtual void SortSkillElements()
        {
            var children = SkillsContainer.transform.Cast<Transform>().Select(x => x.GetComponent<SkillsPanelSkillController>()).Where(x => x != null).ToList();
            children.Sort((a, b) => a.SkillType.CompareTo(b.SkillType));
            for (var i = 0; i < children.Count; ++i)
            {
                children[i].transform.SetSiblingIndex(i);
            }
        }
    }
}
