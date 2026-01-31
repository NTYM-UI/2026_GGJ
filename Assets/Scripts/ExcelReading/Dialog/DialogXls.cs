using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using UnityEngine;
namespace XlsWork.Dialogs
{
    public class DialogXls : MonoBehaviour
    {
        // 改为 List 以支持同 ID 多行（例如多个选项）
        private static Dictionary<int, List<DialogItem>> dialogDict;

        public static Dictionary<int, List<DialogItem>> LoadDialogAsDictionary()
        {
            if (dialogDict != null) return dialogDict; // 已加载则直接返回

            dialogDict = new Dictionary<int, List<DialogItem>>();

            string path = Application.streamingAssetsPath + "/Excel/Dialog.xlsx"; //指定表格的文件路径。在编辑器模式下，Application.dataPath就是Assets文件夹

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            ExcelPackage excel = new ExcelPackage(fs);
            ExcelWorksheet sheet = excel.Workbook.Worksheets[1]; // 读取第一个工作表

            int rowCount = sheet.Dimension.End.Row;//工作表的行数

            // 从第2行开始读取（表格第1行是表头）
            for (int row = 2; row <= rowCount; row++)
            {
                DialogItem item = new DialogItem();

                // 解析表格列（A列到H列，对应1-8）
                item.flag = sheet.Cells[row, 1].Text; // A列：标志
                if (!int.TryParse(sheet.Cells[row, 2].Text, out int id)) continue; // B列：ID，如果解析失败跳过
                
                item.id = id;
                item.character = sheet.Cells[row, 3].Text; // C列：人物
                item.position = sheet.Cells[row, 4].Text; // D列：位置
                item.content = sheet.Cells[row, 5].Text; // E列：内容

                // 处理跳转ID（可能为空，需容错）
                if (int.TryParse(sheet.Cells[row, 6].Text, out int jumpId))
                    item.jumpId = jumpId;
                item.effect = sheet.Cells[row, 7].Text; // G列：效果
                item.target = sheet.Cells[row, 8].Text; // H列：目标

                // I列：延迟时间
                if (float.TryParse(sheet.Cells[row, 9].Text, out float delay))
                {
                    item.delay = delay;
                }
                else
                {
                    item.delay = 0f;
                }

                item.task = sheet.Cells[row, 10].Text;       // J列：任务
                item.optionDesc = sheet.Cells[row, 11].Text; // K列：选项描述

                // 存入字典（支持同 ID 多条数据）
                if (!dialogDict.ContainsKey(item.id))
                {
                    dialogDict[item.id] = new List<DialogItem>();
                }
                dialogDict[item.id].Add(item);
            }
            Debug.Log("对话表格加载完成，共" + dialogDict.Count + "组数据");
            return dialogDict;
        }
    }
}