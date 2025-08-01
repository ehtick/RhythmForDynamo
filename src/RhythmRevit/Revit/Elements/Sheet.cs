﻿using System;
using Autodesk.DesignScript.Runtime;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Dynamo.Graph.Nodes;
using RevitServices.Persistence;
using Revit.Elements;
using RevitServices.Transactions;

namespace Rhythm.Revit.Elements
{
    /// <summary>
    /// Wrapper class for sheets.
    /// </summary>
    public class Sheet
    {
        private Sheet()
        {
        }

        /// <summary>
        /// This node will obtain viewports, views and schedules from a given sheet.
        /// </summary>
        /// <param name="sheet">The sheet to get viewports from.</param>
        /// <returns name="viewports">The viewports on the sheet.</returns>
        /// <returns name="views">The views on the sheet.</returns>
        /// <returns name="schedules">The schedules on the sheet.</returns>
        /// <search>
        /// viewport, schedules,rhythm
        /// </search>
        //this is the node sheet.viewports
        [MultiReturn(new[] {"viewports", "views", "schedules"})]
        [NodeCategory("Query")]
        public static Dictionary<string, object> GetViewportsAndViews(global::Revit.Elements.Views.Sheet sheet)
        {
            Autodesk.Revit.DB.ViewSheet viewSheet = (Autodesk.Revit.DB.ViewSheet) sheet.InternalElement;
            Autodesk.Revit.DB.Document doc = viewSheet.Document;
            //obtains the viewports from the given sheet
            var viewportIds = viewSheet.GetAllViewports();
            List<global::Revit.Elements.Element> viewports = new List<global::Revit.Elements.Element>();
            //attempt to add views to output
            try
            {
                viewports.AddRange(viewportIds.Select(id => doc.GetElement(id).ToDSType(false)));
            }
            catch (Exception)
            {
                //suppress
            }

            //obtains the views from the given sheet
            List<object> views = new List<object>();
            //attempt to add views to output
            try
            {
                views.AddRange(viewports.Select(v =>
                    doc.GetElement(((Autodesk.Revit.DB.Viewport) v.InternalElement).ViewId).ToDSType(false)));
            }
            catch (Exception)
            {
                //suppress
            }

            //obtains the schedules from the sheet
            var scheduleCollector =
                new FilteredElementCollector(doc, viewSheet.Id).OfCategory(BuiltInCategory.OST_ScheduleGraphics)
                    .ToElements();
            List<object> schedules = new List<object>();
            //attempt to add schedules to output
            try
            {
                schedules.AddRange(scheduleCollector.Select(s => s.ToDSType(false)));
            }
            catch (Exception)
            {
                //suppress
            }



            //returns the outputs
            var outInfo = new Dictionary<string, object>
            {
                {"viewports", viewports},
                {"views", views},
                {"schedules", schedules}
            };
            return outInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [IsVisibleInDynamoLibrary(false)]
        public static Autodesk.Revit.DB.Document CurrentDocument()
        {
            return DocumentManager.Instance.CurrentDBDocument;
        }

        /// <summary>
        /// Creates a new sheet.
        /// </summary>
        /// <param name="titleblock">The titleblock to use.</param>
        /// <returns name="Sheet">The newly created sheet.</returns>
        /// <search>
        /// sheet,rhythm
        /// </search>
        [NodeCategory("Create")]
        public static global::Revit.Elements.Element Create(global::Revit.Elements.FamilyType titleblock)
        {
            Autodesk.Revit.DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
            TransactionManager.Instance.EnsureInTransaction(doc);

            var titleBlockName = titleblock.Family.Name;

            var allTitleBlocks = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_TitleBlocks)
                .ToElements();



#if R20 || R21 || R22 || R23
             ElementId titleblockId = new ElementId(titleblock.Id);
#endif

#if R24_OR_GREATER
            var internalTitleblock = titleblock.InternalElement;
            ElementId titleblockId = internalTitleblock.Id;
#endif

            

            global::Revit.Elements.Element createdSheet;
            try
            {
                ViewSheet internalSheet = ViewSheet.Create(doc, titleblockId);
                createdSheet = internalSheet.ToDSType(false);
            }
            catch (Exception e)
            {

                var toUse = allTitleBlocks.Where(t => t.Name.Equals(titleBlockName)).FirstOrDefault().Id;
                ViewSheet internalSheet = ViewSheet.Create(doc, toUse);
                createdSheet = internalSheet.ToDSType(false);
            }
          
            TransactionManager.Instance.TransactionTaskDone();

            return createdSheet;
        }

        /// <summary>
        /// This node will grab the titleblock from the given sheet.
        /// </summary>
        /// <param name="viewSheet">The sheet to get titleblock from.</param>
        /// <returns name="titleblock">The sheet's titleblock.</returns>
        /// <search>
        /// sheet, sheets, titleblock
        /// </search>
        [NodeCategory("Query")]
        public static List<global::Revit.Elements.Element> Titleblock(global::Revit.Elements.Views.Sheet viewSheet)
        {
            var viewId = viewSheet.InternalElement.Id;
            Autodesk.Revit.DB.Document doc = DocumentManager.Instance.CurrentDBDocument;
            FilteredElementCollector elements =
                new FilteredElementCollector(doc, viewId).OfCategory(BuiltInCategory.OST_TitleBlocks);
            //grabs the elements from the collection
            var elementCollection = elements.ToElements();
            //generates a list to convert to a usable type in Dynamo
            List<global::Revit.Elements.Element> ttblList =
                new List<global::Revit.Elements.Element>(elementCollection.Select(e => e.ToDSType(false)).ToArray());

            return ttblList;
        }


    }
}
 