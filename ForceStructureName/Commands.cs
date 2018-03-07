/**
 * Force Structure Name
 * Author: Dean Kertai
 * Date: 2018-03-07
 * 
 * Description:
 * This Civil3D plugin allows you to rename a structure, even if another
 * structure already has that name

 * MIT License
 * Copyright (c) 2018 Dean Kertai

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(ForceStructureName.CommandRoot))]

namespace ForceStructureName
{
	public class CommandRoot
	{
		[CommandMethod("ForceStructureName", CommandFlags.Modal | CommandFlags.UsePickSet)]
		public void Start()
		{
			Document doc = Application.DocumentManager.MdiActiveDocument;
			Editor ed;
			Database db;
			if (doc != null)
			{
				ed = doc.Editor;
				db = doc.Database;

				// Prompt user to select structure
				PromptEntityOptions opts = new PromptEntityOptions("\nSelect a STRUCTURE");
				opts.SetRejectMessage("\nYou must select a STRUCTURE");
				opts.AddAllowedClass(typeof(Autodesk.Civil.DatabaseServices.Structure), true);
				PromptEntityResult res = ed.GetEntity(opts);

				// Check for valid selection
				if (res.Status != PromptStatus.OK || res.ObjectId == ObjectId.Null)
				{
					return;
				}

				// Prompt user for a new name
				PromptStringOptions pso = new PromptStringOptions("\nEnter a new name");
				PromptResult nameResult = ed.GetString(pso);
				if (nameResult.Status != PromptStatus.OK || nameResult.StringResult.Length < 1)
				{
					ed.WriteMessage("\nCanceled. Invalid name");
					return;
				}

				string newName = nameResult.StringResult;


				using (Transaction tr = db.TransactionManager.StartTransaction())
				{
					try
					{
						// Get structure object
						Structure editStructure = tr.GetObject(res.ObjectId, OpenMode.ForWrite, false) as Structure;

						// This list will hold all structures in the drawing, for comparing names
						List<Structure> structures = new List<Structure>();

						// Get all other structures in the drawing
						PromptSelectionResult all = ed.SelectAll();
						SelectionSet ss = all.Value;
						ObjectId[] oids = ss.GetObjectIds();

						// Iterate through all objects in the drawing and save structures to our list
						foreach (ObjectId oid in oids)
						{
							if (oid.ObjectClass.Name.Equals("AeccDbStructure"))
							{
								// Get structure object
								Structure otherStructure = tr.GetObject(oid, OpenMode.ForWrite, false) as Structure;

								// Add to list of structures
								structures.Add(otherStructure);
							}
						}

						// Interate through structure list to check for conflicts
						foreach (Structure checkStructure in structures)
						{
							// Check for conflicts
								if (checkStructure.Name.Equals(newName))
								{
									// Find a new name for the other structure using a counter
									// For example: "S10 (2)"
									int counter = 2;
									string replacementName = String.Format("{0} ({1})", checkStructure.Name, counter);
									while (nameExists(replacementName, structures))
									{
										counter++;
										replacementName = String.Format("{0} ({1})", checkStructure.Name, counter);
									}

									// Update other structure's name and exit
									ed.WriteMessage(
										String.Format(
											"Structure {0} already exists, changing name to {1}",
											checkStructure.Name, replacementName));
									checkStructure.Name = replacementName;
									break;
								}
						}

						// Update the structure name and exit
						string oldName = editStructure.Name;
						editStructure.Name = newName;
						ed.WriteMessage(String.Format("\nStructure {0} changed to {1}", oldName, editStructure.Name));


					}
					catch (System.Exception e)
					{
						ed.WriteMessage(String.Format("\nFailed to update structure name. {0}", e.Message));
					}
					finally
					{
						tr.Commit();
					}
				}

			}
		}


		/**
		 * Check if a name already exists in the given structure list
		 * Returns true if a name already exists
		 */
		private Boolean nameExists(string name, List<Structure> structureList)
		{
			foreach (Structure s in structureList)
			{
				if (s.Name.Equals(name))
				{
					return true;
				}
			}
			return false;
		}
	}

}
