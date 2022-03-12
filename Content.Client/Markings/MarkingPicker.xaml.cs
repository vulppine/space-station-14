using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.CharacterAppearance;
using Content.Client.Stylesheets;
using Content.Shared.Markings;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Markings
{
    [GenerateTypedNameReferences]
    public sealed partial class MarkingPicker : Control
    {
        [Dependency] private readonly MarkingManager _markingManager = default!;
        
        public Action<List<Marking>>? OnMarkingAdded;
        public Action<List<Marking>>? OnMarkingRemoved;
        public Action<List<Marking>>? OnMarkingColorChange;
        public Action<List<Marking>>? OnMarkingRankChange;
        
        private List<Color> _currentMarkingColors = new();

        private ItemList.Item? _selectedMarking;
        private ItemList.Item? _selectedUnusedMarking;
        private MarkingCategories _selectedMarkingCategory = MarkingCategories.Chest;
        private List<Marking> _usedMarkingList = new();

        private List<MarkingCategories> _markingCategories = Enum.GetValues<MarkingCategories>().ToList();

        private string _currentSpecies = "Human"; // mmmmm

        public void SetData(List<Marking> newMarkings, string species)
        {
            _usedMarkingList = newMarkings;
            CMarkingsUsed.Clear();
            CMarkingColors.Visible = false;
            _selectedMarking = null;
            _selectedUnusedMarking = null;
            _currentSpecies = species;
            
            // ugly little O(n * m) loop going on here, but it's more or less
            // all client-side, so whatever
            for (int i = 0; i < _usedMarkingList.Count; i++)
            {
                Marking marking = _usedMarkingList[i];
                if (_markingManager.IsValidMarking(marking, out MarkingPrototype? newMarking))
                {
                    // TODO: Composite sprite preview, somehow.
                    var _item = new ItemList.Item(CMarkingsUsed)
                    {
                        Text = $"{GetMarkingName(newMarking)} ({newMarking.MarkingCategory})", 
                        Icon = newMarking.Sprites[0].Frame0(),
                        Selectable = true,
                        Metadata = newMarking,
                        IconModulate = marking.MarkingColors[0]
                    };
                    CMarkingsUsed.Insert(0, _item);

                    if (marking.MarkingColors.Count != _usedMarkingList[i].MarkingColors.Count)
                    {
                        _usedMarkingList[i] = new Marking(marking.MarkingId, marking.MarkingColors);
                    }
                }

                foreach (var unusedMarking in CMarkingsUnused)
                {
                    if (unusedMarking.Metadata == newMarking)
                    {
                        CMarkingsUnused.Remove(unusedMarking);
                        break;
                    }
                }
            }
        }

        public MarkingPicker()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);

            for (int i = 0; i < _markingCategories.Count; i++)
            {
                CMarkingCategoryButton.AddItem(_markingCategories[i].ToString(), i);
            }
            CMarkingCategoryButton.SelectId(_markingCategories.IndexOf(MarkingCategories.Chest));
            CMarkingCategoryButton.OnItemSelected +=  OnCategoryChange;
            CMarkingsUnused.OnItemSelected += item =>
               _selectedUnusedMarking = CMarkingsUnused[item.ItemIndex];

            CMarkingAdd.OnPressed += args =>
                MarkingAdd();

            CMarkingsUsed.OnItemSelected += OnUsedMarkingSelected;

            CMarkingRemove.OnPressed += args =>
                MarkingRemove();

            CMarkingRankUp.OnPressed += _ => SwapMarkingUp();
            CMarkingRankDown.OnPressed += _ => SwapMarkingDown();
        }

        private string GetMarkingName(MarkingPrototype marking) => Loc.GetString($"{marking.Name}");

        public void Populate()
        {
            CMarkingsUnused.Clear();
            var markings = _markingManager.CategorizedMarkings();
            foreach (var marking in markings[_selectedMarkingCategory])
            {
                if (_usedMarkingList.Contains(marking.AsMarking())) continue;
                if (!marking.SpeciesRestrictions.Contains(_currentSpecies) && !marking.Unrestricted) continue;
                var item = CMarkingsUnused.AddItem($"{GetMarkingName(marking)}", marking.Sprites[0].Frame0());
                item.Metadata = marking;
            }
        }

        private void SwapMarkingUp()
        {
            if (_selectedMarking == null)
            {
                return;
            }

            var i = CMarkingsUsed.IndexOf(_selectedMarking);
            if (ShiftMarkingRank(i, -1))
            {
                OnMarkingRankChange?.Invoke(_usedMarkingList);
            }
        }

        private void SwapMarkingDown()
        {
            if (_selectedMarking == null)
            {
                return;
            }

            var i = CMarkingsUsed.IndexOf(_selectedMarking);
            if (ShiftMarkingRank(i, 1))
            {
                OnMarkingRankChange?.Invoke(_usedMarkingList);
            }
        }

        private bool ShiftMarkingRank(int src, int places)
        {
            if (src + places >= CMarkingsUsed.Count || src + places < 0)
            {
                return false;
            }

            var visualDest = src + places; // what it would visually look like

            var visualTemp = CMarkingsUsed[visualDest];
            CMarkingsUsed[visualDest] = CMarkingsUsed[src];
            CMarkingsUsed[src] = visualTemp;

            var backingSrc = _usedMarkingList.Count - 1 - src; // what it actually needs to be
            var backingDest = backingSrc - places; // what it actually needs to be

            var backingTemp = _usedMarkingList[backingDest];

            _usedMarkingList[backingDest] = _usedMarkingList[backingSrc];
            _usedMarkingList[backingSrc] = backingTemp;
            
            return true;
        }

        // repopulate in case markings are restricted,
        // and also filter out any markings that are now invalid
        public void SetSpecies(string species)
        {
            _currentSpecies = species;
            var markingCount = _usedMarkingList.Count;
            for (int i = 0; i < _usedMarkingList.Count; i++)
            {
                var markingPrototype = (MarkingPrototype) CMarkingsUsed[i].Metadata!;
                if (!markingPrototype.SpeciesRestrictions.Contains(species))
                {
                    _usedMarkingList.RemoveAt(i);
                }

            }

            if (markingCount != CMarkingsUsed.Count)
            {
                OnMarkingRemoved?.Invoke(_usedMarkingList);
            }

            Populate();
        }

        private void OnCategoryChange(OptionButton.ItemSelectedEventArgs category)
        {
            CMarkingCategoryButton.SelectId(category.Id);
            _selectedMarkingCategory = _markingCategories[category.Id];
            Populate();
        }

        private void OnUsedMarkingSelected(ItemList.ItemListSelectedEventArgs item)
        {
            _selectedMarking = CMarkingsUsed[item.ItemIndex];
            var prototype = (MarkingPrototype) _selectedMarking.Metadata!;
            _currentMarkingColors.Clear();
            CMarkingColors.RemoveAllChildren();
            List<List<ColorSlider>> colorSliders = new();
            for (int i = 0; i < prototype.Sprites.Count; i++)
            {
                var colorContainer = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                };

                CMarkingColors.AddChild(colorContainer);

                List<ColorSlider> sliders = new();
                ColorSlider colorSliderR = new ColorSlider(StyleNano.StyleClassSliderRed);
                ColorSlider colorSliderG = new ColorSlider(StyleNano.StyleClassSliderGreen);
                ColorSlider colorSliderB = new ColorSlider(StyleNano.StyleClassSliderBlue);

                var rsi = (SpriteSpecifier.Rsi) prototype.Sprites[i];
                var name = Loc.GetString($"{prototype.Name}-{rsi.RsiState}");
                colorContainer.AddChild(new Label { Text = $"{name} color:" });
                colorContainer.AddChild(colorSliderR);
                colorContainer.AddChild(colorSliderG);
                colorContainer.AddChild(colorSliderB);

                var currentColor = new Color(
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].RByte,
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].GByte,
                    _usedMarkingList[item.ItemIndex].MarkingColors[i].BByte
                );
                _currentMarkingColors.Add(currentColor);
                int colorIndex = _currentMarkingColors.IndexOf(currentColor);

                colorSliderR.ColorValue = currentColor.RByte;
                colorSliderG.ColorValue = currentColor.GByte;
                colorSliderB.ColorValue = currentColor.BByte;

                Action colorChanged = delegate()
                {
                    _currentMarkingColors[colorIndex] = new Color(
                        colorSliderR.ColorValue,
                        colorSliderG.ColorValue,
                        colorSliderB.ColorValue
                    );

                    ColorChanged(colorIndex);
                };
                colorSliderR.OnValueChanged += colorChanged;
                colorSliderG.OnValueChanged += colorChanged;
                colorSliderB.OnValueChanged += colorChanged;
            }

            CMarkingColors.Visible = true;
        }

        private void ColorChanged(int colorIndex)
        {
            if (_selectedMarking is null) return;
            var markingPrototype = (MarkingPrototype) _selectedMarking.Metadata!;
            int markingIndex = _usedMarkingList.FindIndex(m => m.MarkingId == markingPrototype.ID);

            if (markingIndex < 0) return;

            _selectedMarking.IconModulate = _currentMarkingColors[colorIndex];
            _usedMarkingList[markingIndex].SetColor(colorIndex, _currentMarkingColors[colorIndex]);
            OnMarkingColorChange?.Invoke(_usedMarkingList);
        }

        private void MarkingAdd()
        {
            if (_usedMarkingList is null || _selectedUnusedMarking is null) return;

            MarkingPrototype marking = (MarkingPrototype) _selectedUnusedMarking.Metadata!;
            _usedMarkingList.Add(marking.AsMarking());

            CMarkingsUnused.Remove(_selectedUnusedMarking);
            var item = new ItemList.Item(CMarkingsUsed)
            {
                Text = $"{GetMarkingName(marking)} ({marking.MarkingCategory})", 
                Icon = marking.Sprites[0].Frame0(),
                Selectable = true,
                Metadata = marking,
            };
            CMarkingsUsed.Insert(0, item);

            _selectedUnusedMarking = null;
            OnMarkingAdded?.Invoke(_usedMarkingList);
        }

        private void MarkingRemove()
        {
            if (_usedMarkingList is null || _selectedMarking is null) return;

            MarkingPrototype marking = (MarkingPrototype) _selectedMarking.Metadata!;
            _usedMarkingList.Remove(marking.AsMarking());
            CMarkingsUsed.Remove(_selectedMarking);

            if (marking.MarkingCategory == _selectedMarkingCategory)
            {
                var item = CMarkingsUnused.AddItem($"{GetMarkingName(marking)}", marking.Sprites[0].Frame0());
                item.Metadata = marking;
            }
            _selectedMarking = null;
            CMarkingColors.Visible = false;
            OnMarkingRemoved?.Invoke(_usedMarkingList);
        }
    }
}
