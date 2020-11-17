using System.Collections.Generic;
using System.Windows.Media;

namespace ScadaProject
{
    class ChildControls
    {
        private List<object> childrenList;

        public List<object> GetChildren(Visual parent, int degreeOfKinship)
        {
            childrenList = new List<object>();
            GetChildControls(parent, degreeOfKinship);

            return childrenList;
        }

        private void GetChildControls(Visual parent, int degreeOfKinship)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(parent, i);
                childrenList.Add(child);

                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    GetChildControls(child, degreeOfKinship + 1);
                }
            }
        }
    }
}
