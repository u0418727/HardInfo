using System;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;

namespace Hardlnfo
{
    public partial class Form1 : Form
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter memUsageCounter;
        private PerformanceCounter memFreeCounter;
        private Dictionary<string, PerformanceCounter> diskFreeCounters;
        private Dictionary<string, float> diskTotalSizes; // Для хранения общего размера каждого диска
        private Timer timer;

        public Form1()
        {
            InitializeComponent();
            InitializeCPUCounter();
            InitializeMemoryCounters();
            InitializeDiskCounters();
            InitializeTimer();
        }

        private void InitializeCPUCounter()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        }

        private void InitializeMemoryCounters()
        {
            // Используемая память
            memUsageCounter = new PerformanceCounter("Memory", "Committed Bytes", null, true);
            // Свободная память
            memFreeCounter = new PerformanceCounter("Memory", "Available MBytes", null, true);
        }

        private void InitializeDiskCounters()
        {
            diskFreeCounters = new Dictionary<string, PerformanceCounter>();
            diskTotalSizes = new Dictionary<string, float>();
            var driveQuery = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3");

            foreach (ManagementObject drive in driveQuery.Get())
            {
                var name = drive["Name"].ToString();
                diskFreeCounters[name] = new PerformanceCounter("LogicalDisk", "Free Megabytes", name, true);

                // Получение и сохранение общего размера диска
                ulong totalSize = (ulong)drive["Size"] / (1024 * 1024); // Преобразование в мегабайты
                diskTotalSizes[name] = totalSize;
            }
        }

        private void InitializeTimer()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (sender, e) =>
            {
                hardwarePartInfo.Items.Clear();
                UpdateCPUUsage();
                UpdateMemoryUsage();
                UpdateDiskUsage();
            };
        }

        private void UpdateCPUUsage()
        {
            float cpuUsage = cpuCounter.NextValue();
            ListViewItem item = new ListViewItem("Загрузка процессора");
            item.SubItems.Add($"{cpuUsage:N2}%");
            UpdateOrAddListViewItem(item, "CPU");
        }

        private void UpdateMemoryUsage()
        {
            float memUsage = memUsageCounter.NextValue() / (1024 * 1024 * 1024); // Преобразование в гигабайты
            float memFree = memFreeCounter.NextValue();
            ListViewItem itemUsage = new ListViewItem("Используемая память");
            itemUsage.SubItems.Add($"{memUsage:N2} GB");
            ListViewItem itemFree = new ListViewItem("Свободная память");
            itemFree.SubItems.Add($"{memFree:N2} MB");

            UpdateOrAddListViewItem(itemUsage, "MemoryUsed");
            UpdateOrAddListViewItem(itemFree, "MemoryFree");
        }

        private void UpdateDiskUsage()
        {
            foreach (var disk in diskFreeCounters.Keys)
            {
                float freeSpace = diskFreeCounters[disk].NextValue(); // Свободное пространство в мегабайтах
                float totalSize = diskTotalSizes[disk];
                float usedSpace = totalSize - freeSpace; // Рассчитываем занятое пространство

                // Создаем элемент для отображения информации о диске
                ListViewItem item = new ListViewItem($"Диск {disk}");
                item.SubItems.Add($"Занято: {usedSpace:N0} MB");
                item.SubItems.Add($"Свободно: {freeSpace:N0} MB");
                item.SubItems.Add($"Всего: {totalSize:N0} MB");

                // Обновляем или добавляем элемент в ListView
                UpdateOrAddListViewItem(item, $"Disk {disk}");
            }
        }

        private void UpdateOrAddListViewItem(ListViewItem item, string key)
        {
            var existingItem = hardwarePartInfo.Items.Find(key, false);
            if (existingItem.Length > 0)
                hardwarePartInfo.Items[hardwarePartInfo.Items.IndexOf(existingItem[0])] = item;
            else
                hardwarePartInfo.Items.Add(item);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            hardwarePart.SelectedIndex = 0; // Выбор первого элемента в списке
            UpdateCPUUsage();
            UpdateMemoryUsage();
            UpdateDiskUsage();
        }
        private void GetHardWareInfo(string key, ListView list)
        {
            list.Items.Clear();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + key);

            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    ListViewGroup listViewGroup;

                    try
                    {
                        listViewGroup = list.Groups.Add(obj["Name"].ToString(), obj["Name"].ToString());
                    }
                    catch (Exception ex)
                    {
                        listViewGroup = list.Groups.Add(obj.ToString(), obj.ToString());
                    }

                    foreach (PropertyData data in obj.Properties)
                    {
                        ListViewItem item = new ListViewItem(listViewGroup);
                        item.UseItemStyleForSubItems = false;
                        item.BackColor = list.Items.Count % 2 == 0 ? Color.WhiteSmoke : Color.White;
                        item.Text = data.Name;

                        if (data.Value != null && !string.IsNullOrEmpty(data.Value.ToString()))
                        {
                            string displayValue = ConvertDataToString(data);
                            item.SubItems.Add(displayValue);
                            list.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void hardwarePart_SelectedIndexChanged(object sender, EventArgs e)
        {
            string key = "";
            switch (hardwarePart.SelectedItem.ToString())
            {
                case "Процессор":
                    key = "Win32_Processor";
                    break;
                case "Видеокарта":
                    key = "Win32_VideoController";
                    break;
                case "Сокет":
                    key = "Win32_IDEController";
                    break;
                case "Батарея":
                    key = "Win32_Battery";
                    break;
                case "Биос":
                    key = "Win32_BIOS";
                    break;
                case "Оперативная память":
                    key = "Win32_PhysicalMemory";
                    break;
                case "Кэш":
                    key = "Win32_CacheMemory";
                    break;
                case "USB":
                    key = "Win32_USBController";
                    break;
                case "Диск":
                    key = "Win32_DiskDrive";
                    break;
                case "Логические диски":
                    key = "Win32_LogicalDisk";
                    break;
                case "Клавиатура":
                    key = "Win32_Keyboard";
                    break;
                case "Сеть":
                    key = "Win32_NetworkAdapter";
                    break;
                case "Пользователи":
                    key = "Win32_Account";
                    break;
                case "Динамическая информация":
                    timer.Start();
                    return;
                default:
                    key = "";
                    break;
            }
            timer.Stop();
            if (key != "")
                GetHardWareInfo(key, hardwarePartInfo);
        }

        private string ConvertDataToString(PropertyData data)
        {
            switch (data.Value.GetType().ToString())
            {
                case "System.String[]":
                    return string.Join(" ", (string[])data.Value);
                case "System.UInt16[]":
                    return string.Join(" ", Array.ConvertAll((ushort[])data.Value, Convert.ToString));
                default:
                    return data.Value.ToString();
            }
        }
    }
}