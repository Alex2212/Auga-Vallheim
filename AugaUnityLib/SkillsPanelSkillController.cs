using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class SkillsPanelSkillController : MonoBehaviour
    {
        public Skills.SkillType SkillType = Skills.SkillType.None;
        public Image Icon;
        public Image ProgressBarLevel;
        public Image ProgressBarAccumulator;
        public Text NameText;
        public Text LevelText;
        public float StartFill;
        public float EndFill;

        protected SkillTooltip _skillTooltip;

        public void Awake()
        {
            _skillTooltip = GetComponent<SkillTooltip>();
        }

        public virtual void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var skills = player.GetSkills();
            if (skills.m_skillData.TryGetValue(SkillType, out var skillData))
            {
                if (_skillTooltip != null) _skillTooltip.Skill = skillData;

                Icon.sprite = skillData.m_info.m_icon;
                NameText.text = Localization.instance.Localize("$skill_" + SkillType.ToString().ToLower());
                LevelText.text = Localization.instance.Localize("$level") + $" {skillData.m_level:0}";
                ProgressBarLevel.fillAmount = Mathf.Lerp(StartFill, EndFill, skillData.m_level / 100f);
                ProgressBarAccumulator.fillAmount = Mathf.Lerp(StartFill, EndFill, skillData.GetLevelPercentage());
            }
            else
            {
                var definition = skills.m_skills.FirstOrDefault(d => d.m_skill == SkillType);
                if (definition == null) return;
                Icon.sprite = definition.m_icon;
                NameText.text = Localization.instance.Localize("$skill_" + SkillType.ToString().ToLower());
                LevelText.text = Localization.instance.Localize("$level") + " 0";
                ProgressBarLevel.fillAmount = ProgressBarAccumulator.fillAmount = StartFill;
            }
        }

        public virtual void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                Update();
            }
        }
    }
}
