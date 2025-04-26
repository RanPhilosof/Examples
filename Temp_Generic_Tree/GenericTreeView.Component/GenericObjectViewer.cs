using GenericTreeView.SharedTypes;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MudBlazor.CategoryTypes;
using static MudBlazor.Colors;

namespace GenericTreeView.Component
{
    public class MudBlazorCustomTreeView : TreeItemData<GenericObjectNode>
    {
        public MudBlazorCustomTreeView OneOfNode { get; set; }
        public bool ShouldBeShownBecauseOneOf()
        {
            if (OneOfNode != null && OneOfNode.Value != null)
            {
                //if (OneOfNode.Value.PropertyValue is Enum)
                {
                    var enumString = OneOfNode.Value.PropertyValue.ToString();
                    if (this.Value.PropertyName != enumString)
                        return false;

                    //var names = Enum.GetNames(OneOfNode.Value.Type);
                    //OneofCase
                }

            }
            
            return true;
        }

        public void HandleOneOf()
        {            
            HandleOneOf(this);
        }

        private void HandleOneOf(MudBlazorCustomTreeView mudBlazorCustomTreeView)
        {
            if (mudBlazorCustomTreeView != null && mudBlazorCustomTreeView.Value != null)
            {
                Dictionary<string, Dictionary<string, MudBlazorCustomTreeView>> oneOfsAtThisLevel = new Dictionary<string, Dictionary<string, MudBlazorCustomTreeView>>();

                foreach (var child in mudBlazorCustomTreeView.Value.PropertyChildren)
                {                    
                    if (child.NameTypeText.EndsWith("OneofCase"))
                    {
                        oneOfsAtThisLevel.Add(child.NameTypeText, new Dictionary<string, MudBlazorCustomTreeView>());

                        var names = Enum.GetNames(child.Type);
                        foreach (var name in names)
                            oneOfsAtThisLevel[child.NameTypeText].Add(name, (MudBlazorCustomTreeView) child.GetAggregator());
                    }
                }

                foreach (var child in mudBlazorCustomTreeView.Value.PropertyChildren)
                    if (!child.NameTypeText.EndsWith("OneofCase"))
                        foreach (var oneOfsAtThisLvl in oneOfsAtThisLevel.Values)
                            if (oneOfsAtThisLvl.ContainsKey(child.PropertyName))
                                ((MudBlazorCustomTreeView)child.GetAggregator()).OneOfNode = oneOfsAtThisLvl[child.PropertyName];

                foreach (var child in mudBlazorCustomTreeView.Value.PropertyChildren)
                    HandleOneOf((MudBlazorCustomTreeView)child.GetAggregator());
            }
        }


        public bool IsVisible { get; set; } = true;
        public override string? Text { get { return Value == null ? string.Empty : Value.PropertyName; } }
        public virtual string? Icon { get; set; }

        public virtual bool Expandable { get { return Children != null && Children.Count > 0; } } //return Value.PropertyChildren.Count > 0; } }

        public override List<TreeItemData<GenericObjectNode>>? Children { 
            get 
            {
                return Value is null ? null : 
                    Value.PropertyChildren.Where(
                        x =>
                        {
                            var agg = ((MudBlazorCustomTreeView)x.GetAggregator());

                            return agg.IsVisible && agg.ShouldBeShownBecauseOneOf();
                        })
                        .Select(x => (TreeItemData<GenericObjectNode>)x.GetAggregator()).ToList(); 
            } 
        }

        public override bool HasChildren => Value != null && Value.PropertyChildren.Count > 0;

        //public bool IsEnum { get { return Value.PropertyValue is Enum; } }

        //public object? Value { get { return PropertyValue; } }

        //public virtual bool Expanded { get; set; }

        //public virtual bool Selected { get; set; }
    }
}
