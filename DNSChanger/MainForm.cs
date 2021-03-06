﻿using DNSChanger.Structs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSChanger
{
    public partial class MainForm : Form
    {
        private Interface? _interfaceToValidate;
        private bool _ignoreDnsComboChanges;

        public MainForm()
        {
            InitializeComponent();
            Text = GlobalVars.Name;
            Icon = Properties.Resources.Icon;


            _interfaceToValidate = DNSValidate.GetInterfaceToValidate();
            if (_interfaceToValidate.HasValue)
            {
                validateCb.Checked = true;
                validateLbl.Text = _interfaceToValidate.Value.Name;
            }


            interfacesCombo.SelectedIndexChanged += InterfacesComboOnSelectedIndexChanged;
            dnsCombo.SelectedIndexChanged += DnsComboOnSelectedIndexChanged;
            v4primary.TextChanged += (sender, args) => MatchInputToDnsServer();
            v4secondary.TextChanged += (sender, args) => MatchInputToDnsServer();
            v6primary.TextChanged += (sender, args) => MatchInputToDnsServer();
            v6secondary.TextChanged += (sender, args) => MatchInputToDnsServer();
            validateCb.CheckedChanged += validateCb_CheckedChanged;
            LatencyTester.LatencyUpdated += (sender, args) => RefreshDnsLatency();
            

            FillDnsCombo();
            FillInterfacesCombo();
            ActiveControl = v4primary;
            

            var testTask = new Task(() => { LatencyTester.Run(GlobalVars.DnsServers, 5); });
            testTask.Start();
        }


        private Interface GetSelectedInterface()
        {
            return (Interface) ((ComboBoxItem) interfacesCombo.SelectedItem).Value;
        }

        private void FillInterfacesCombo()
        {
            interfacesCombo.Items.Clear();

            foreach (var @interface in NetshHelper.GetInterfaces())
            {
                interfacesCombo.Items.Add(new ComboBoxItem(@interface.ToString(), @interface));
            }

            interfacesCombo.SelectedIndex = 0;
        }
        
        private void FillDnsCombo()
        {
            dnsCombo.Items.Clear();
            dnsCombo.Items.Add(new ComboBoxItem("Custom", null));

            foreach (var dnsServer in GlobalVars.DnsServers)
            {
                dnsCombo.Items.Add(new ComboBoxItem(dnsServer.ToString(), dnsServer));
            }

            dnsCombo.SelectedIndex = 0;
        }

        private void RefreshDnsLatency()
        {
            Invoke((MethodInvoker) delegate
            {
                _ignoreDnsComboChanges = true;

                for (var i = 0; i < dnsCombo.Items.Count; i++)
                {
                    var item = (ComboBoxItem) dnsCombo.Items[i];
                    if (item.Value == null) continue;

                    var dnsServerEntryItem = (DNSServerEntry) item.Value;
                    var dnsServerEntry = GlobalVars.DnsServers.First(d => d.Name == dnsServerEntryItem.Name);

                    dnsCombo.Items[i] = new ComboBoxItem(dnsServerEntry.ToString(), dnsServerEntry);
                }

                _ignoreDnsComboChanges = false;
            });
        }

        private void MatchInputToDnsServer()
        {
            _ignoreDnsComboChanges = true;

            var v4primary = this.v4primary.Text.Trim();
            var v4secondary = this.v4secondary.Text.Trim();
            var v6primary = this.v6primary.Text.Trim();
            var v6secondary = this.v6secondary.Text.Trim();

            dnsCombo.SelectedIndex = 0;

            for (var i = 0; i < dnsCombo.Items.Count; i++)
            {
                var success = true;

                var item = (ComboBoxItem) dnsCombo.Items[i];
                if (item.Value == null) continue;

                var dnsServer = (DNSServerEntry) item.Value;
                
                var dnsV4 = dnsServer.DnsEntries.Where(d => d.IsV4).ToList();
                var dnsV6 = dnsServer.DnsEntries.Where(d => d.IsV6).ToList();

                for (var j = 0; j < Math.Min(2, dnsV4.Count); j++)
                {
                    if (v4primary != dnsV4[j].Value && v4secondary != dnsV4[j].Value)
                    {
                        // fail
                        success = false;
                        break;
                    }
                }

                if (!success)
                {
                    continue;
                }

                for (var j = 0; j < Math.Min(2, dnsV6.Count); j++)
                {
                    if (v6primary != dnsV6[j].Value && v6secondary != dnsV6[j].Value)
                    {
                        // fail
                        success = false;
                        break;
                    }
                }

                if (!success)
                {
                    continue;
                }

                // success
                dnsCombo.SelectedIndex = i;
                break;
            }

            _ignoreDnsComboChanges = false;
        }


        private void InterfacesComboOnSelectedIndexChanged(object sender, EventArgs e)
        {
            v4primary.Text = null;
            v4secondary.Text = null;
            v6primary.Text = null;
            v6secondary.Text = null;
            
            var @interface = GetSelectedInterface();
            var dnsEntries = NetshHelper.GetDnsEntries(@interface);

            var dnsV4 = dnsEntries.Where(d => d.IsV4).ToList();
            var dnsV6 = dnsEntries.Where(d => d.IsV6).ToList();

            for (var i = 0; i < Math.Min(2, dnsV4.Count); i++)
            {
                switch (i)
                {
                    case 0:
                        v4primary.Text = dnsV4[i].Value;
                        break;
                    case 1:
                        v4secondary.Text = dnsV4[i].Value;
                        break;
                }
            }

            for (var i = 0; i < Math.Min(2, dnsV6.Count); i++)
            {
                switch (i)
                {
                    case 0:
                        v6primary.Text = dnsV6[i].Value;
                        break;
                    case 1:
                        v6secondary.Text = dnsV6[i].Value;
                        break;
                }
            }
            
            MatchInputToDnsServer();
        }

        private void DnsComboOnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreDnsComboChanges) return;

            var item = (ComboBoxItem) dnsCombo.SelectedItem;
            if (item.Value == null) return;
            
            var dnsServer = (DNSServerEntry) item.Value;
            var dnsEntries = dnsServer.DnsEntries;

            var dnsV4 = dnsEntries.Where(d => d.IsV4).ToList();
            var dnsV6 = dnsEntries.Where(d => d.IsV6).ToList();

            for (var i = 0; i < Math.Min(2, dnsV4.Count); i++)
            {
                switch (i)
                {
                    case 0:
                        v4primary.Text = dnsV4[i].Value;
                        break;
                    case 1:
                        v4secondary.Text = dnsV4[i].Value;
                        break;
                }
            }

            for (var i = 0; i < Math.Min(2, dnsV6.Count); i++)
            {
                switch (i)
                {
                    case 0:
                        v6primary.Text = dnsV6[i].Value;
                        break;
                    case 1:
                        v6secondary.Text = dnsV6[i].Value;
                        break;
                }
            }
        }


        private void saveBtn_Click(object sender, EventArgs e)
        {
            var dnsEntries = new List<DNSEntry>(4);
            
            var v4primary = this.v4primary.Text.Trim();
            var v4secondary = this.v4secondary.Text.Trim();
            var v6primary = this.v6primary.Text.Trim();
            var v6secondary = this.v6secondary.Text.Trim();

            if (!string.IsNullOrEmpty(v4primary) && Regex.IsMatch(v4primary, @"^(\d+?[\.]?){4}$"))
            {
                dnsEntries.Add(new DNSEntry(v4primary));
            }

            if (!string.IsNullOrEmpty(v4secondary) && Regex.IsMatch(v4secondary, @"^(\d+?[\.]?){4}$"))
            {
                dnsEntries.Add(new DNSEntry(v4secondary));
            }

            if (!string.IsNullOrEmpty(v6primary) && Regex.IsMatch(v6primary, @"^([0-9a-f]{0,4}:){1,7}[0-9a-f]{0,4}$", RegexOptions.IgnoreCase))
            {
                dnsEntries.Add(new DNSEntry(v6primary));
            }

            if (!string.IsNullOrEmpty(v6secondary) && Regex.IsMatch(v6secondary, @"^([0-9a-f]{0,4}:){1,7}[0-9a-f]{0,4}$", RegexOptions.IgnoreCase))
            {
                dnsEntries.Add(new DNSEntry(v6secondary));
            }
            
            var @interface = GetSelectedInterface();
            NetshHelper.UpdateDnsEntries(@interface, dnsEntries);
            NetshHelper.FlushDns();
            UpdateDnsValidation();
            
            ButtonSuccessAnimation(sender);
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            InterfacesComboOnSelectedIndexChanged(interfacesCombo, null);
            
            ButtonSuccessAnimation(sender);
        }

        private void resetBtn_Click(object sender, EventArgs e)
        {
            var @interface = GetSelectedInterface();
            NetshHelper.ResetDnsEntries(@interface);
            NetshHelper.FlushDns();

            InterfacesComboOnSelectedIndexChanged(interfacesCombo, null);
            UpdateDnsValidation();

            ButtonSuccessAnimation(sender);
        }

        private void ButtonSuccessAnimation(object sender)
        {
            var btn = sender as Button;
            btn.BackColor = Color.LightGreen;

            var thr = new Thread(() =>
            {
                Thread.Sleep(600);
                btn.BackColor = SystemColors.Control;
            }) {IsBackground = true};
            thr.Start();
        }


        private void validateCb_CheckedChanged(object sender, EventArgs e)
        {
            if (validateCb.Checked)
            {
                var interSelectForm = new InterfaceSelectionForm();
                interSelectForm.ShowDialog(this);

                if (!interSelectForm.Success) return;
                var @interface = interSelectForm.SelectedInterface;

                _interfaceToValidate = @interface;
                validateLbl.Text = _interfaceToValidate.Value.Name;

                UpdateDnsValidation();
            }
            else
            {
                _interfaceToValidate = null;
                validateLbl.Text = null;
                
                UpdateDnsValidation();
            }
        }

        private void UpdateDnsValidation()
        {
            if (_interfaceToValidate.HasValue)
            {
                DNSValidate.Enable(_interfaceToValidate.Value);
            }
            else
            {
                DNSValidate.Disable();
            }
        }
    }
}
