using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CreateWallElevation
{
    public partial class CreateWallElevationWPF : Window
    {
        Document Doc;
        List<ViewFamilyType> ViewFamilyTypeList;
        List<ViewSection> ViewSectionTemplateList;

        public ViewFamilyType SelectedViewFamilyType;
        public bool UseTemplate;
        public ViewSection ViewSectionTemplate;
        public string SelectedBuildByName;
        public string SelectedUseToBuildName;
        public double Indent;
        public double IndentUp;
        public double IndentDown;
        public double ProjectionDepth;
        public int CurveNumberOfSegments;
        public ViewSheet SelectedViewSheet;
        public CreateWallElevationWPF(Document doc, List<ViewSheet> viewSheetList)
        {
            Doc = doc;
            InitializeComponent();
            
            comboBox_PlaceOnSheet.ItemsSource = viewSheetList;
            if (viewSheetList.Count != 0)
            {
                comboBox_PlaceOnSheet.SelectedItem = comboBox_PlaceOnSheet.Items[0];
            }
        }

        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void CreateWallElevationWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                DialogResult = true;
                Close();
            }

            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void UseToBuildCheckedChanged(object sender, RoutedEventArgs e)
        {
            string useToBuildSelectedName = (groupBox_UseToBuild.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            if (useToBuildSelectedName == "rbt_Section")
            {
                ViewFamilyTypeList = new FilteredElementCollector(Doc)
                    .OfClass(typeof(ViewFamilyType))
                    .WhereElementIsElementType()
                    .Cast<ViewFamilyType>()
                    .Where(vft => vft.ViewFamily == ViewFamily.Section)
                    .OrderBy(vft => vft.Name, new AlphanumComparatorFastString())
                    .ToList();
            }
            if (useToBuildSelectedName == "rbt_Facade")
            {
                ViewFamilyTypeList = new FilteredElementCollector(Doc)
                    .OfClass(typeof(ViewFamilyType))
                    .WhereElementIsElementType()
                    .Cast<ViewFamilyType>()
                    .Where(vft => vft.ViewFamily == ViewFamily.Elevation)
                    .OrderBy(vft => vft.Name, new AlphanumComparatorFastString())
                    .ToList();
            }

            comboBox_SelectTypeSectionFacade.ItemsSource = ViewFamilyTypeList;
            comboBox_SelectTypeSectionFacade.DisplayMemberPath = "Name";
            if(ViewFamilyTypeList.Count != 0)
            {
                comboBox_SelectTypeSectionFacade.SelectedItem = comboBox_SelectTypeSectionFacade.Items[0];
            }
        }

        private void checkBox_UseTemplate_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBox_UseTemplate.IsChecked)
            {
                comboBox_UseTemplate.IsEnabled = true;
                ViewSectionTemplateList = new FilteredElementCollector(Doc)
                    .OfClass(typeof(ViewSection))
                    .Cast<ViewSection>()
                    .Where(vs => vs.IsTemplate == true)
                    .OrderBy(vft => vft.Name, new AlphanumComparatorFastString())
                    .ToList();

                comboBox_UseTemplate.ItemsSource = ViewSectionTemplateList;
                comboBox_UseTemplate.DisplayMemberPath = "Name";
                if (ViewSectionTemplateList.Count != 0)
                {
                    comboBox_UseTemplate.SelectedItem = comboBox_UseTemplate.Items[0];
                }
            }
            else
            {
                comboBox_UseTemplate.IsEnabled = false;
            }

        }
        private void SaveSettings()
        {
            SelectedViewFamilyType = comboBox_SelectTypeSectionFacade.SelectedItem as ViewFamilyType;
            SelectedBuildByName = (groupBox_BuildBy.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            SelectedUseToBuildName = (groupBox_UseToBuild.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
#if R2019 || R2020 || R2021

            double.TryParse(textBox_Indent.Text, out Indent);
            Indent = UnitUtils.ConvertToInternalUnits(Indent, DisplayUnitType.DUT_MILLIMETERS);

            double.TryParse(textBox_IndentUp.Text, out IndentUp);
            IndentUp = UnitUtils.ConvertToInternalUnits(IndentUp, DisplayUnitType.DUT_MILLIMETERS);

            double.TryParse(textBox_IndentDown.Text, out IndentDown);
            IndentDown = UnitUtils.ConvertToInternalUnits(IndentDown, DisplayUnitType.DUT_MILLIMETERS);

            double.TryParse(textBox_ProjectionDepth.Text, out ProjectionDepth);
            ProjectionDepth = UnitUtils.ConvertToInternalUnits(ProjectionDepth, DisplayUnitType.DUT_MILLIMETERS);
#else
            double.TryParse(textBox_Indent.Text, out Indent);
            Indent = UnitUtils.ConvertToInternalUnits(Indent, UnitTypeId.Millimeters);

            double.TryParse(textBox_IndentUp.Text, out IndentUp);
            IndentUp = UnitUtils.ConvertToInternalUnits(IndentUp, UnitTypeId.Millimeters);

            double.TryParse(textBox_IndentDown.Text, out IndentDown);
            IndentDown = UnitUtils.ConvertToInternalUnits(IndentDown, UnitTypeId.Millimeters);

            double.TryParse(textBox_ProjectionDepth.Text, out ProjectionDepth);
            ProjectionDepth = UnitUtils.ConvertToInternalUnits(ProjectionDepth, UnitTypeId.Millimeters);
#endif
            UseTemplate = (bool)checkBox_UseTemplate.IsChecked;
            if (UseTemplate)
            {
                ViewSectionTemplate = comboBox_UseTemplate.SelectedItem as ViewSection;
            }

            Int32.TryParse(textBox_CurveNumberOfSegments.Text, out CurveNumberOfSegments);

            SelectedViewSheet = comboBox_PlaceOnSheet.SelectedItem as ViewSheet;
        }
    }
}
