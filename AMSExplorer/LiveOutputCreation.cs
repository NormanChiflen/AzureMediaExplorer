﻿//----------------------------------------------------------------------------------------------
//    Copyright 2018 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//---------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.Azure.Management.Media.Models;

namespace AMSExplorer
{
    public partial class LiveOutputCreation : Form
    {
        public string ChannelName;
        private AMSClientV3 _client;

        public string ProgramName
        {
            get { return textboxprogramname.Text; }
            set { textboxprogramname.Text = value; }
        }

        public string ProgramDescription
        {
            get { return textBoxDescription.Text; }
            set { textBoxDescription.Text = value; }
        }


        public TimeSpan archiveWindowLength
        {
            get
            {
                return new TimeSpan((int)numericUpDownArchiveHours.Value, (int)numericUpDownArchiveMinutes.Value, 0);
            }
            set
            {
                numericUpDownArchiveHours.Value = value.Hours;
                numericUpDownArchiveMinutes.Value = value.Minutes;
            }
        }


        public bool IsReplica
        {
            get
            {
                return checkBoxReplica.Checked;
            }
        }


        public string ReplicaLocatorID
        {
            get { return labelLocatorID.Text; }

        }

        public string ForceManifestName
        {
            get
            {
                if (checkBoxReplica.Checked)
                {
                    return labelManifestFile.Text;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(textBoxManifestName.Text))
                    {
                        return null;
                    }
                    else
                    {
                        return textBoxManifestName.Text.Trim();
                    }
                }
            }
        }

        public short? HLSFragmentPerSegment
        {
            get
            {
                return checkBoxHLSFragPerSegDefined.Checked ? (short?)numericUpDownHLSFragPerSeg.Value : null;
            }
            set
            {
                if (value != null)
                    numericUpDownHLSFragPerSeg.Value = (short)value;
            }
        }

        public string AssetName
        {
            get { return textBoxAssetName.Text; }
            set { textBoxAssetName.Text = value; }
        }

        public bool CreateLocator
        {
            get { return checkBoxCreateLocator.Checked; }
            set { checkBoxCreateLocator.Checked = value; }
        }

        public bool EnableDynEnc
        {
            get { return checkBoxDynEnc.Checked; }
            set { checkBoxDynEnc.Checked = value; }
        }

        public string StorageSelected
        {
            get { return ((Item)comboBoxStorage.SelectedItem).Value; }
        }

        public LiveOutputCreation(AMSClientV3 client)
        {
            InitializeComponent();
            this.Icon = Bitmaps.Azure_Explorer_ico;
            _client = client;
        }

        private void LiveOutputCreation_Load(object sender, EventArgs e)
        {
            this.Text = string.Format(this.Text, ChannelName);
            checkBoxCreateLocator.Text = string.Format(checkBoxCreateLocator.Text, Properties.Settings.Default.DefaultLocatorDurationDaysNew);
            labelManifestFile.Text = string.Empty;
            labelLocatorID.Text = string.Empty;
            labelURLFileNameWarning.Text = string.Empty;

            foreach (var storage in _client.credentialsEntry.MediaService.StorageAccounts.ToList())
            {
                bool primary = (storage.Type == StorageAccountType.Primary);

                comboBoxStorage.Items.Add(new Item(storage.Id.Split('/').Last() + (primary ? " (primary)" : ""), storage.Id.Split('/').Last()));
                if (primary) comboBoxStorage.SelectedIndex = comboBoxStorage.Items.Count - 1;
            }

            checkProgramName();
        }

        private void checkBoxReplica_CheckedChanged(object sender, EventArgs e)
        {
            textBoxProgramSourceURL.Enabled = checkBoxReplica.Checked;
            textBoxManifestName.Enabled = checkBoxReplica.Checked;
            if (checkBoxReplica.Checked)
            {
                checkBoxCreateLocator.Checked = true;
                checkBoxCreateLocator.Enabled = false;
            }
            else
            {
                checkBoxCreateLocator.Enabled = true;
            }
        }

        private void textBoxIProgramSourceURL_TextChanged(object sender, EventArgs e)
        {
            string filename = null;
            string locId = null;
            bool Error = false;
            string url = textBoxProgramSourceURL.Text;
            if (url.EndsWith("/manifest", StringComparison.OrdinalIgnoreCase))
            {
                url = url.ToLower().Replace("/manifest", string.Empty);
            }
            try
            {
                Uri myUri = new Uri(url);
                filename = System.IO.Path.GetFileNameWithoutExtension((myUri).LocalPath);
                locId = System.IO.Path.GetDirectoryName((myUri).LocalPath);//.Replace(@"\", Constants.LocatorIdPrefix);
            }
            catch
            {
                Error = true;
                labelURLFileNameWarning.Text = AMSExplorer.Properties.Resources.CreateProgram_textBoxIProgramSourceURL_TextChanged_URLCannotBeAnalyzed;
                labelManifestFile.Text = string.Empty;
                labelLocatorID.Text = string.Empty;
            }

            if (!Error)
            {
                labelURLFileNameWarning.Text = string.Empty;
                labelManifestFile.Text = filename;
                labelLocatorID.Text = locId;
            }
        }

        internal static bool IsProgramNameValid(string name)
        {
            Regex reg = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9- ]{0,254}[a-zA-Z0-9])?$", RegexOptions.Compiled);
            return (reg.IsMatch(name));
        }

        private void checkProgramName()
        {
            TextBox tb = textboxprogramname;

            if (!IsProgramNameValid(tb.Text))
            {
                errorProvider1.SetError(tb, AMSExplorer.Properties.Resources.CreateProgram_checkProgramName_ProgramNameIsNotValid);
            }
            else
            {
                errorProvider1.SetError(tb, String.Empty);
            }
        }

        private void textboxprogramname_TextChanged(object sender, EventArgs e)
        {
            checkProgramName();
        }
    }
}