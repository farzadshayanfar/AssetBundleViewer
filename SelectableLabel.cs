using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AssetBundleViewer
{
    public class SelectableLabel : VisualElement
    {
        private static readonly string _sLabelUSS = "selectable-label";
        private static readonly string _sLabelSelectedUSS = "selectable-label--selected";
        private static readonly string _sLabelLabelUSS = "selectable-label-label";

        private string _aUxmlResource;

        private readonly StyleSheet _defaultStyles =
            Resources.Load<StyleSheet>("DefaultStyleSheets/SelectableLabel");

        private bool _isSelected;


        private Label _label = new();

        public string LabelText
        {
            get => _label.text;
            set
            {
                _label.text = value;
                _label.style.display = value != string.Empty ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        public SelectableLabel()
        {
            Add(_label);
            AddToClassList(_sLabelUSS);
            _label.AddToClassList(_sLabelLabelUSS);
            this.AddManipulator(new Clickable(delegate ()
            {
                if (IsSelected)
                    IsSelected = false;
                else
                    IsSelected = true;
            }));

            styleSheets.Add(_defaultStyles);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value)
                    AddToClassList(_sLabelSelectedUSS);

                else
                    RemoveFromClassList(_sLabelSelectedUSS);


                _isSelected = value;
            }
        }

        public new class UxmlFactory : UxmlFactory<SelectableLabel, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _mLabelText = new()
            { name = "label-text", defaultValue = string.Empty };
            private readonly UxmlStringAttributeDescription _mIconPath = new()
            { name = "icon-path", defaultValue = string.Empty };
            private readonly UxmlBoolAttributeDescription _mIsSelected = new()
            { name = "Is-selected", defaultValue = false };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is SelectableLabel sLabel)
                {
                    sLabel.IsSelected = _mIsSelected.GetValueFromBag(bag, cc);
                    sLabel.LabelText = _mLabelText.GetValueFromBag(bag, cc);
                }
            }
        }
    }
}