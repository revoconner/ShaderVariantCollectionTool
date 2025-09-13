using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShaderVariantsCollectionTool
{
    public class VariantFilter_Keyword : IVariantFilter
    {
        private ShaderVariantCollectionToolConfig mConfig;

        public void SetConfig(ShaderVariantCollectionToolConfig config)
        {
            mConfig = config;
        }

        public override bool Filter(ShaderVariantCollection.ShaderVariant variant)
        {
            if (mConfig == null)
                return true;

            var keywordFilters = mConfig.GetKeywordFilters();
            var variantKeywords = variant.keywords;

            foreach (var filter in keywordFilters)
            {
                if (!filter.enabled || string.IsNullOrWhiteSpace(filter.keywords))
                    continue;

                var filterKeywords = filter.keywords.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                if (filterKeywords.Length == 0)
                    continue;

                if (filter.includeAllCombinations)
                {
                    // Exclude if variant contains ALL filter keywords
                    if (filterKeywords.All(filterKeyword => variantKeywords.Contains(filterKeyword)))
                    {
                        return false;
                    }
                }
                else
                {
                    // Exclude if variant keywords exactly match filter keywords (order independent)
                    if (variantKeywords.Length == filterKeywords.Length &&
                        filterKeywords.All(filterKeyword => variantKeywords.Contains(filterKeyword)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}