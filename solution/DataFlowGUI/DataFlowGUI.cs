using NF.Tools.DataFlow;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.Serialization;

namespace DataFlowGUI
{
    public partial class DataFlowGUIForm : Form
    {
        DataFlowRunnerOption _opt = new DataFlowRunnerOption();
        const string DATAFLOW_YAML = "dataflow.yaml";

        public DataFlowGUIForm()
        {
            InitializeComponent();
            this.menu_browse_src.Click += new System.EventHandler(this.OnBtnBrowseSrc_Click);
            this.menu_browse_dst.Click += new System.EventHandler(this.OnBtnBrowseDst_Click);
            this.menu_go.Click += new System.EventHandler(this.OnBtnGo_Click);
            this.menu_exit.Click += new System.EventHandler((o, s) => this.Close());
            if (File.Exists(DATAFLOW_YAML))
            {
                string configYamlStr = File.ReadAllText(DATAFLOW_YAML);
                IDeserializer deserializer = new DeserializerBuilder().Build();
                DataFlowRunnerOption yaml = deserializer.Deserialize<DataFlowRunnerOption>(configYamlStr);
                _opt = yaml;

                if (_opt != null)
                {
                    if (_opt.input_paths != null)
                    {
                        foreach (string inputPaths in _opt.input_paths)
                        {
                            list_excel.Items.Add(inputPaths);
                        }
                    }
                    txt_dst.Text = _opt.output_db_path;
                    if (File.Exists(_opt.output_db_path))
                    {
                        btn_reveal.Enabled = true;
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _opt.input_paths = GetInputPathsOrNull(list_excel);
            _opt.@namespace = "AutoGenerated.DB";
            _opt.output_db_path = txt_dst.Text;

            try
            {
                ISerializer serializer = new SerializerBuilder().Build();
                string configYamlStr = serializer.Serialize(_opt);
                File.WriteAllText(DATAFLOW_YAML, configYamlStr);
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

        private void OnListExcel_DragDrop(object sender, DragEventArgs e)
        {
            bool isFileDrop = e.Data.GetDataPresent(DataFormats.FileDrop);
            if (!isFileDrop)
            {
                return;
            }

            object dropData = e.Data.GetData(DataFormats.FileDrop);
            if (dropData == null)
            {
                return;
            }

            string[] dropPaths = dropData as string[];
            if (dropPaths == null)
            {
                return;
            }

            foreach (string excelPath in DataFlowRunner.GetExcelFpaths(dropPaths))
            {
                list_excel.Items.Add(excelPath);
            }
        }

        private void OnListExcel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void OnListExcel_DragOver(object sender, DragEventArgs e)
        {

        }

        private void OnBtnRemove_Click(object sender, System.EventArgs e)
        {
            while (list_excel.SelectedItems.Count > 0)
            {
                list_excel.Items.Remove(list_excel.SelectedItem);
            }
        }

        private void OnBtnBrowseSrc_Click(object sender, System.EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Filter = "excel files (*.xlsx)|*.xlsx";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                foreach (string fileName in dlg.FileNames)
                {
                    list_excel.Items.Add(fileName);
                }
            }
        }

        List<string> GetInputPathsOrNull(ListBox listBox)
        {
            int listCount = listBox.Items.Count;
            if (listCount == 0)
            {
                return null;
            }

            List<string> ret = new List<string>(listBox.Items.Count);
            foreach (object item in listBox.Items)
            {
                ret.Add(item.ToString());
            }
            return ret;
        }

        private void OnBtnGo_Click(object sender, System.EventArgs e)
        {
            List<string> inputPaths = GetInputPathsOrNull(list_excel);
            if (inputPaths == null)
            {
                return;
            }

            _opt.input_paths = inputPaths;
            _opt.@namespace = "AutoGenerated.DB";
            _opt.output_db_path = txt_dst.Text;
            progress_export.Value = 0;

            int ret = DataFlowRunner.Run(_opt);
            progress_export.Value = 100;
        }

        private void OnBtnBrowseDst_Click(object sender, System.EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "db files (*.db)|*.db";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                txt_dst.Text = dlg.FileName;
            }
        }

        private void OnBtnReveal_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(_opt.output_db_path))
            {
                return;
            }
            if (!File.Exists(_opt.output_db_path))
            {
                return;
            }
            string dirName = Path.GetDirectoryName(_opt.output_db_path);
            Process.Start("explorer.exe", dirName);
        }

        private void OnMenuInfo_Click(object sender, System.EventArgs e)
        {
            AboutForm f = new AboutForm();
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog();
        }
    }
}