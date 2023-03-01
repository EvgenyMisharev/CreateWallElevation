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

        CreateWallElevationSettings CreateWallElevationSettingsItem;
        public CreateWallElevationWPF(Document doc, List<ViewSheet> viewSheetList)
        {
            Doc = doc;
            CreateWallElevationSettingsItem = new CreateWallElevationSettings().GetSettings();
            InitializeComponent();
            comboBox_PlaceOnSheet.ItemsSource = viewSheetList;

            if (CreateWallElevationSettingsItem != null)
            {
                if(CreateWallElevationSettingsItem.SelectedBuildByName == "rbt_ByRoom")
                {
                    rbt_ByRoom.IsChecked = true;
                }
                else
                {
                    rbt_ByWall.IsChecked = true;
                }

                if (CreateWallElevationSettingsItem.SelectedUseToBuildName == "rbt_Section")
                {
                    rbt_Section.IsChecked = true;
                }
                else
                {
                    rbt_Facade.IsChecked = true;
                }

                if (ViewFamilyTypeList.Count != 0)
                {
                    if (ViewFamilyTypeList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.SelectedViewFamilyTypeName) != null)
                    {
                        comboBox_SelectTypeSectionFacade.SelectedItem = ViewFamilyTypeList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.SelectedViewFamilyTypeName);
                    }
                    else
                    {
                        comboBox_SelectTypeSectionFacade.SelectedItem = comboBox_SelectTypeSectionFacade.Items[0];
                    }
                }

                if (CreateWallElevationSettingsItem.UseTemplate == true)
                {
                    checkBox_UseTemplate.IsChecked = true;

                    if (ViewSectionTemplateList.Count != 0)
                    {
                        if (ViewSectionTemplateList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.ViewSectionTemplateName) != null)
                        {
                            comboBox_UseTemplate.SelectedItem = ViewSectionTemplateList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.ViewSectionTemplateName);
                        }
                        else
                        {
                            comboBox_UseTemplate.SelectedItem = comboBox_UseTemplate.Items[0];
                        }
                    }
                }

                textBox_Indent.Text = CreateWallElevationSettingsItem.Indent;
                textBox_IndentUp.Text = CreateWallElevationSettingsItem.IndentUp;
                textBox_IndentDown.Text = CreateWallElevationSettingsItem.IndentDown;
                textBox_ProjectionDepth.Text = CreateWallElevationSettingsItem.ProjectionDepth;
                textBox_CurveNumberOfSegments.Text = CreateWallElevationSettingsItem.CurveNumberOfSegments;

                if (viewSheetList.Count != 0)
                {
                    if (viewSheetList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.SelectedViewSheetName) != null)
                    {
                        comboBox_PlaceOnSheet.SelectedItem = viewSheetList.FirstOrDefault(vft => vft.Name == CreateWallElevationSettingsItem.SelectedViewSheetName);
                    }
                    else
                    {
                        comboBox_PlaceOnSheet.SelectedItem = comboBox_PlaceOnSheet.Items[0];
                    }
                }
            }
            else
            {
                if (viewSheetList.Count != 0)
                {
                    comboBox_PlaceOnSheet.SelectedItem = comboBox_PlaceOnSheet.Items[0];
                }
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
            CreateWallElevationSettingsItem = new CreateWallElevationSettings();

            SelectedViewFamilyType = comboBox_SelectTypeSectionFacade.SelectedItem as ViewFamilyType;
            if(SelectedViewFamilyType != null)
            {
                CreateWallElevationSettingsItem.SelectedViewFamilyTypeName = SelectedViewFamilyType.Name;
            }

            SelectedBuildByName = (groupBox_BuildBy.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            CreateWallElevationSettingsItem.SelectedBuildByName = SelectedBuildByName;

            SelectedUseToBuildName = (groupBox_UseToBuild.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;
            CreateWallElevationSettingsItem.SelectedUseToBuildName = SelectedUseToBuildName;

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
            CreateWallElevationSettingsItem.Indent = textBox_Indent.Text;
            CreateWallElevationSettingsItem.IndentUp = textBox_IndentUp.Text;
            CreateWallElevationSettingsItem.IndentDown = textBox_IndentDown.Text;
            CreateWallElevationSettingsItem.ProjectionDepth = textBox_ProjectionDepth.Text;

            UseTemplate = (bool)checkBox_UseTemplate.IsChecked;
            CreateWallElevationSettingsItem.UseTemplate = UseTemplate;
            if (UseTemplate)
            {
                ViewSectionTemplate = comboBox_UseTemplate.SelectedItem as ViewSection;
                CreateWallElevationSettingsItem.ViewSectionTemplateName = ViewSectionTemplate.Name;
            }

            Int32.TryParse(textBox_CurveNumberOfSegments.Text, out CurveNumberOfSegments);
            CreateWallElevationSettingsItem.CurveNumberOfSegments = textBox_CurveNumberOfSegments.Text;

            SelectedViewSheet = comboBox_PlaceOnSheet.SelectedItem as ViewSheet;
            CreateWallElevationSettingsItem.SelectedViewSheetName = SelectedViewSheet.Name;

            CreateWallElevationSettingsItem.SaveSettings();
        }
    }
}
