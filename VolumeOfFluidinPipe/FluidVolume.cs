using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NXOpen;
using NXOpen.UF;
using NXOpen.Features;
using NXOpen.Assemblies;

namespace VolumeOfFluidinPipe
{
    public class FluidVolume
    {
        private static Session theSession = Session.GetSession();
        private static UFSession theUFSession = UFSession.GetUFSession();
        private static UI theUI = UI.GetUI();
        private static Part workPart = theSession.Parts.Work;
        private static Part displayPart = theSession.Parts.Display;
        private static ListingWindow lw = theSession.ListingWindow;
        public static void Main (string[] args)
        {          
            // Get the root component of the Assembly
            Component rootComponent = workPart.ComponentAssembly.RootComponent;

            // Check weather Assembly file is open or not
            if(rootComponent != null){

                // Get the Part of the rootComponentPart
                Part rootComponentPart = (Part)rootComponent.Prototype.OwningPart;

                //calling LoadAssemblyFull function to load all the sub parts of Assembly
                LoadAssemblyFull(rootComponent);

                // Declaration of the fluidVolumeOfPipe 
                double fluidVolumeOfPipe = 0;

                // Looping trough the child components of the rootComponents
                foreach (Component childComponent in rootComponent.GetChildren())
                {
                    // Get the Part of the childComponent
                    Part childPart = (Part)childComponent.Prototype.OwningPart;

                    // Calling OpenAssemblyChildFile function to open the Child Part in new tab
                    OpenAssemblyChildFile(childPart);

                    // Calling VolumeOfFluid Function to fluid volume of pipe(Child Part)
                    VolumeOfFluid(out double volume);

                    // Add volume(fluid volume of childComponent) to fluidVolumeOfPipe
                    fluidVolumeOfPipe = fluidVolumeOfPipe + volume;

                    // Calling CloseAssemblyChildFile function to Close the open Child Part in new tab
                    CloseAssemblyChildFile(rootComponentPart);
                    
                }
                           
                // Get the Unit System of the open Part                
                UnitCollection unitCollection = workPart.UnitCollection;
                string[] measureType = unitCollection.GetMeasures();
                Unit[] units = unitCollection.GetMeasureTypes("Volume");
                Unit baseUits = unitCollection.GetBase("Volume");

                // Convert cubic millimeter to liters
                double volumeInLiters = fluidVolumeOfPipe / 1000000;

                // Round off the Volume to 3 decimals units
                fluidVolumeOfPipe = Math.Round(fluidVolumeOfPipe, 3);
                volumeInLiters = Math.Round(volumeInLiters, 3);

                // Open the listing window to show the results
                lw.Open();                
                lw.WriteLine("Coolant required " + fluidVolumeOfPipe.ToString()+" "+ baseUits.Abbreviation);
                lw.WriteLine("Coolant required " + volumeInLiters.ToString() + " Liters");
            }
            else
            {
                theUI.NXMessageBox.Show("Information", NXMessageBox.DialogType.Information, "It is not an Assembly File");
            }

        }

        public static int GetUnloadOption(string dummy)
        {
            return (int)NXOpen.Session.LibraryUnloadOption.Immediately;
        }


        private static bool VolumeOfFluid(out double volumeOfFluid)
        {
            volumeOfFluid = 0; // Initialization out variable volumeOfFluid
            try
            {
                // Declaration of Faces to store start and end Faces of Pipe
                Face inFace = null, singleDirectionStopFace = null;
                // Declaration of Edges to store the inner and outer edges of Circular face
                Edge inEdge = null, outEdge = null;
                // Declaration of Dictionary to store the Faces in respective Directions
                Dictionary<Face, double> faceInXDirection = new Dictionary<Face, double>();
                Dictionary<Face, double> faceInYDirection = new Dictionary<Face, double>();
                Dictionary<Face, double> faceInZDirection = new Dictionary<Face, double>();

                // Looping through the Bodies of the workPart
                foreach (Body body in workPart.Bodies)
                {
                    // Looping through the Faces of the body
                    foreach (Face face in body.GetFaces())
                    {
                        // Condition to check for find Planar faces of body
                        if (face.SolidFaceType == Face.FaceType.Planar)
                        {
                            // To store the out Variable of FindFaceDirection function
                            double[] point = new double[3];
                            // To store the out Variable of FindFaceDirection function
                            double[] FaceDetails = new double[3];
                            // Calling FindFaceDirection function
                            FindFaceDirection(face, out FaceDetails, out point);

                            // Condition to check Faces in +Ve and -Ve X Direction
                            if (Math.Round(FaceDetails[0]) == 1 || Math.Round(FaceDetails[0]) == -1)
                            {
                                faceInXDirection.Add(face, point[0]); // Add to Dictionary
                            }
                            // Condition to check Faces in +Ve and -Ve Y Direction
                            else if (Math.Round(FaceDetails[1]) == 1 || Math.Round(FaceDetails[1]) == -1)
                            {
                                faceInYDirection.Add(face, point[1]); // Add to Dictionary
                            }
                            // Condition to check Faces in +Ve and -Ve Z Direction
                            else if (Math.Round(FaceDetails[2]) == 1 || Math.Round(FaceDetails[2]) == -1)
                            {
                                faceInZDirection.Add(face, point[2]); // Add to Dictionary
                            }
                        }
                    }
                }

                // Order the Faces in  Dictionary with respective location from Coordinate system.
                faceInXDirection = faceInXDirection.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                faceInYDirection = faceInYDirection.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
                faceInZDirection = faceInZDirection.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                // Condition to find the start and end Faces of Pipe and assign those faces
                // to inFace and singleDirectionStopFace
                {
                    // Check for faceInXDirection Dictionary is not empty
                    if (faceInXDirection.Count > 0)
                    {
                        // Check for faceInYDirection and faceInZDirection Dictionary are empty
                        // That means given Pipe Start and end faces in X Direction only 
                        if (faceInYDirection.Count == 0 && faceInZDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInXDirection.Keys.First();
                            singleDirectionStopFace = faceInXDirection.Keys.Last();
                        }
                        // Check for faceInYDirection Dictionary is not empty and faceInZDirection Dictionary is empty
                        // That means given Pipe Start and end faces in X and Y Directions
                        else if (faceInYDirection.Count > 0 && faceInZDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInXDirection.Keys.First();
                            singleDirectionStopFace = faceInYDirection.Keys.Last();
                        }
                        // Check for faceInYDirection Dictionary is empty and faceInZDirection Dictionary is not empty
                        // That means given Pipe Start and end faces in X and Z Directions
                        else if (faceInYDirection.Count == 0 && faceInZDirection.Count > 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInXDirection.Keys.First();
                            singleDirectionStopFace = faceInZDirection.Keys.Last();
                        }
                    }
                    // Check for faceInYDirection Dictionary is not empty
                    else if (faceInYDirection.Count > 0)
                    {
                        // Check for faceInXDirection and faceInZDirection Dictionary are empty
                        // That means given Pipe Start and end faces in Y Direction only
                        if (faceInXDirection.Count == 0 && faceInZDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInYDirection.Keys.First();
                            singleDirectionStopFace = faceInYDirection.Keys.Last();
                        }
                        // Check for faceInXDirection Dictionary is not empty and faceInZDirection Dictionary is empty
                        // That means given Pipe Start and end faces in X and Y Directions
                        else if (faceInXDirection.Count > 0 && faceInZDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInYDirection.Keys.First();
                            singleDirectionStopFace = faceInXDirection.Keys.Last();
                        }
                        // Check for faceInXDirection Dictionary is empty and faceInZDirection Dictionary is not empty
                        // That means given Pipe Start and end faces in Y and Z Directions
                        else if (faceInXDirection.Count == 0 && faceInZDirection.Count > 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInYDirection.Keys.First();
                            singleDirectionStopFace = faceInZDirection.Keys.Last();
                        }
                    }
                    // Check for faceInZDirection Dictionary is not empty
                    else if (faceInZDirection.Count > 0)
                    {
                        // Check for faceInXDirection and faceInYDirection Dictionary are empty
                        // That means given Pipe Start and end faces in Z Direction only
                        if (faceInXDirection.Count == 0 && faceInYDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInZDirection.Keys.First();
                            singleDirectionStopFace = faceInZDirection.Keys.Last();
                        }
                        // Check for faceInXDirection Dictionary is not empty and faceInYDirection Dictionary is empty
                        // That means given Pipe Start and end faces in X and Z Directions
                        else if (faceInXDirection.Count > 0 && faceInYDirection.Count == 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInZDirection.Keys.First();
                            singleDirectionStopFace = faceInXDirection.Keys.Last();
                        }
                        // Check for faceInXDirection Dictionary is empty and faceInYDirection Dictionary is not empty
                        // That means given Pipe Start and end faces in Y and Z Directions
                        else if (faceInXDirection.Count == 0 && faceInYDirection.Count > 0)
                        {
                            // Assign Faces to their respective Variables
                            inFace = faceInZDirection.Keys.First();
                            singleDirectionStopFace = faceInYDirection.Keys.Last();
                        }
                    }
                }

                // Calling GetFaceEdge Function to get the edges of Circular Face
                GetFaceEdge(inFace, out outEdge, out inEdge);

                // List to store the Collected Face Edges for filter
                List<Edge> filterEdges = new List<Edge>();
                // Add inEdge and outEdge to filterEdges List
                filterEdges.Add(inEdge);
                filterEdges.Add(outEdge);

                // Declaration of Face array to store outEdge Faces
                Face[] inFacesofPipe = outEdge.GetFaces();

                // List to store the outer Faces of Pipe for selection and filter 
                List<Face> outerFaces = new List<Face>();
                // Add outEdge Faces to outerFaces List
                outerFaces.AddRange(inFacesofPipe);

                // Declaration of checker to stop do-while loop
                bool checker = true;
                do
                {
                    // Calling RequiredEdgesFaces function 
                    RequiredEdgesFaces(outerFaces, filterEdges, singleDirectionStopFace, out Edge reqEdge, out Face reqFace);
                    // Check for reqEdge is not null
                    if (reqEdge != null)
                        filterEdges.Add(reqEdge); // Add reqEdge to filterEdges List

                    // Check for reqFace is not null
                    if (reqFace != null)
                        outerFaces.Add(reqFace); // Add reqFace to outerFaces List

                    // Check for reqEdge is null
                    if (reqEdge == null)
                        checker = false; // Checker to false to stop do-while loop

                } while (checker);

                //outerFaces.ElementAt(1).Highlight();
                //outerFaces.ElementAt(outerFaces.Count - 1).Highlight();

                // Undo Mark ID to delete the deleteFaceFeature
                NXOpen.Session.UndoMarkId markId1;
                markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "DeleteFaces");

                // Calling DeleteFaces function to Delete the outer faces of pipe
                DeleteFaces(outerFaces, out NXOpen.Features.Feature deleteFaceFeature);

                // Looping through the bodies of the Pipe
                foreach (Body body in workPart.Bodies)
                {
                    // Code to get the Volume of given inner fluid body of Pipe
                    MeasureManager measureManager = workPart.MeasureManager;
                    IBody[] iBody = { body };
                    Unit[] volumeUnits = new Unit[1];
                    volumeUnits[0] = workPart.UnitCollection.GetBase("Volume");
                    MeasureBodies measureBodies = measureManager.NewMassProperties(volumeUnits, 0.99, iBody);

                    // Assign Volume of inner fluid of Pipe to volumeOfFluid
                    volumeOfFluid = measureBodies.Volume;
                }

                // Code to delete the Delete Face Feature 

                bool notifyOnDelete1;
                notifyOnDelete1 = theSession.Preferences.Modeling.NotifyOnDelete;

                theSession.UpdateManager.ClearErrorList();

                NXOpen.TaggedObject[] deleteObjects = new NXOpen.TaggedObject[1];
                // Add the deleteFaceFeature to deleteObjects array
                deleteObjects[0] = deleteFaceFeature;
                int nErrs1;
                nErrs1 = theSession.UpdateManager.AddObjectsToDeleteList(deleteObjects);

                // Update the Delete ID using markID
                int nErrs2;
                nErrs2 = theSession.UpdateManager.DoUpdate(markId1);

                //// End of the Delect code

                //lw.WriteLine("Inner Volume of Fulid is " + volumeOfBody.ToString());

                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }
        }

        /// <summary>
        /// To Get Edges of given Circular Face
        /// </summary>
        /// <param name="face">Circular face</param>
        /// <param name="outerEdge">Outer Edge of Given Face</param>
        /// <param name="innerEdge">Inner Edge of Given Face</param>
        /// <returns></returns>
        private static bool GetFaceEdge(Face face, out Edge outerEdge, out Edge innerEdge)
        {
            // Initialization out variable
            outerEdge = null;
            innerEdge = null;
            try
            {
                // Declaration of Dictionary to store the inner and outer edges of Circular face
                Dictionary<Edge, double> edgeinfo = new Dictionary<Edge, double>();

                // looping through the edges of given Face
                foreach (Edge edges in face.GetEdges())
                {
                    // Using UF function of UF_Eval_AskArc find the radius of the Edge
                    IntPtr evaluator;
                    UFEval.Arc arcData;
                    theUFSession.Eval.Initialize(edges.Tag, out evaluator);
                    theUFSession.Eval.AskArc(evaluator, out arcData);
                    double edgeRadius = arcData.radius;

                    // Add to Dictionary
                    edgeinfo.Add(edges, edgeRadius);
                }

                // Order the Edges in Dictionary with respective Radius value
                edgeinfo = edgeinfo.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                // Assign edges to their respective Variables
                innerEdge = edgeinfo.Keys.First();
                outerEdge = edgeinfo.Keys.Last();

                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }
        }

        /// <summary>
        /// To Get the Outer Edge of given Circular Face
        /// </summary>
        /// <param name="face">Circular face</param>
        /// <param name="edge">Outer Edge of Given Face</param>
        /// <returns></returns>
        private static bool GetFaceEdge(Face face, out Edge edge)
        {
            edge = null;
            try
            {
                Dictionary<Edge, double> edgeinfo = new Dictionary<Edge, double>();
                foreach (Edge edge1 in face.GetEdges())
                {
                    IntPtr evaluator;
                    UFEval.Arc arcData;
                    theUFSession.Eval.Initialize(edge1.Tag, out evaluator);
                    theUFSession.Eval.AskArc(evaluator, out arcData);
                    double edgeRadius = arcData.radius;

                    edgeinfo.Add(edge1, edgeRadius);
                }
                edgeinfo = edgeinfo.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                edge = edgeinfo.Keys.Last();

                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }
        }
        /// <summary>
        /// To Get the common Edge and Face from given list of Faces and Edges
        /// </summary>
        /// <param name="faces">Face List</param>
        /// <param name="edges">Edge List</param>
        /// <param name="stopFace">Other side Face</param>
        /// <param name="requiredEdge">out Common Face</param>
        /// <param name="requiredFace">out Common Edge</param>
        /// <returns></returns>
        private static bool RequiredEdgesFaces(List<Face> faceList, List<Edge> edgeList, Face stopFace, out Edge requiredEdge, out Face requiredFace)
        {
            // Initialization out variable
            requiredEdge = null;
            requiredFace = null;
            try
            {
                // looping through the face of given faces List
                foreach (Face faces in faceList)
                {
                    // Check for Stop Face(Outer Face of Pipe)
                    if (faces != stopFace)
                    {
                        // looping through the edge of faces
                        foreach (Edge edges in faces.GetEdges())
                        {
                            // Check for edge not Contain in given edgeList
                            if (!edgeList.Contains(edges))
                            {
                                requiredEdge = edges; // Assign to edge requiredEdge
                            }
                        }
                    }
                    // faces is same face of stopFace(Outer Face of Pipe)
                    else
                    {
                        requiredFace = faces; // Assign to edge requiredEdge
                    }
                }

                // Check for requiredEdge is not null
                if (requiredEdge != null)
                {
                    // looping through the faces of requiredEdge
                    foreach (Face face in requiredEdge.GetFaces())
                    {
                        // Check for face not Contain in given faceList
                        if (!faceList.Contains(face))
                        {
                            requiredFace = face; // Assign to face requiredFace
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
                //throw;
            }
        }
        /// <summary>
        /// To get the common Edge and Face from given list of Faces and Edges
        /// </summary>
        /// <param name="faces">Face List</param>
        /// <param name="edges">Edge List</param>
        /// <param name="requiredEdge">out Common Face</param>
        /// <param name="requiredFace">out Common Edge</param>
        /// <returns></returns>
        private static bool RequiredEdgesFaces(List<Face> faces, List<Edge> edges, out Edge requiredEdge, out Face requiredFace)
        {
            requiredEdge = null;
            requiredFace = null;
            try
            {
                foreach (Face face in faces)
                {
                    if (face.SolidFaceType != Face.FaceType.Planar)
                    {
                        foreach (Edge edge in face.GetEdges())
                        {
                            if (!edges.Contains(edge))
                            {
                                requiredEdge = edge;
                            }
                        }
                    }
                }

                if (requiredEdge != null)
                {
                    foreach (Face face in requiredEdge.GetFaces())
                    {
                        if (!faces.Contains(face))
                        {
                            requiredFace = face;
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
                //throw;
            }
        }
        /// <summary>
        /// Delete the Selected Faces
        /// </summary>
        /// <param name="faces">Faces to Delete</param>
        /// <param name="deleteFaceFeature">Out as Delete Feature</param>
        /// <returns></returns>
        private static bool DeleteFaces(List<Face> faceList, out NXOpen.Features.Feature deleteFaceFeature)
        {
            // Initialization out variable
            deleteFaceFeature = null;
            try
            {
                // Declaration of CreateDeleteFaceBuilder
                NXOpen.Features.Feature nullNXOpen_Features_Feature = null;
                NXOpen.Features.DeleteFaceBuilder deleteFaceBuilder;
                deleteFaceBuilder = workPart.Features.CreateDeleteFaceBuilder(nullNXOpen_Features_Feature);

                NXOpen.Point3d origin = new NXOpen.Point3d(0.0, 0.0, 0.0);
                NXOpen.Vector3d normal = new NXOpen.Vector3d(0.0, 0.0, 1.0);
                NXOpen.Plane plane;
                plane = workPart.Planes.CreatePlane(origin, normal, NXOpen.SmartObject.UpdateOption.WithinModeling);

                deleteFaceBuilder.CapPlane = plane;

                deleteFaceBuilder.FaceEdgeBlendPreference = NXOpen.Features.DeleteFaceBuilder.FaceEdgeBlendPreferenceOptions.Cliff;

                NXOpen.Plane nullNXOpen_Plane = null;
                deleteFaceBuilder.CapPlane = nullNXOpen_Plane;

                // Assign faceList to facesArray
                NXOpen.Face[] facesArray = faceList.ToArray(); 
                NXOpen.FaceDumbRule faceDumbRule;
                faceDumbRule = workPart.ScRuleFactory.CreateRuleFaceDumb(facesArray);

                NXOpen.SelectionIntentRule[] rules = new NXOpen.SelectionIntentRule[1];
                rules[0] = faceDumbRule;
                deleteFaceBuilder.FaceCollector.ReplaceRules(rules, false);

                deleteFaceBuilder.Type = NXOpen.Features.DeleteFaceBuilder.SelectTypes.Face;

                // Commit the Builder
                NXOpen.NXObject nXObject1;
                nXObject1 = deleteFaceBuilder.Commit();

                // Assign the deleteFaceBuilder Feature to deleteFaceFeature
                deleteFaceFeature = (NXOpen.Features.Feature)nXObject1;

                // Destroy deleteFaceBuilder
                deleteFaceBuilder.Destroy();

                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }
        }
        /// <summary>
        /// To Find the Direction of the given Face
        /// </summary>
        /// <param name="face">Pass the Face for which need to find the Direction</param>
        /// <param name="direction">Out Dirction as Double Array of Size 3</param>
        ///  <param name="point">Out Point as Double Array of Size 3</param>
        /// <returns></returns>
        private static bool FindFaceDirection(Face face, out double[] direction, out double[] point)
        {
            direction = new double[3]; // Declaration of direction Array
            point = new double[3]; // Declaration of point Array
            /* In Direction [] Array 
                * If Direction [0] and equal to the "1" means X Direction in +ve side
                * If Direction [0] and equal to the "-1" means X Direction in -ve side
                * If Direction [1] and equal to the "1" means Y Direction in +ve side
                * If Direction [1] and equal to the "-1" means Y Direction in -ve side                 * 
                * If Direction [2] and equal to the "1" means Z Direction in +ve side
                * If Direction [2] and equal to the "-1" means Z Direction in -ve side

               eg: if (Math.Round(FaceDir[1], 1) == 1)
                       {ReqFaceFillet = faces;}
            */
            try
            {
                int type; // Declaration of Type
                //double[] point = new double[3]; // Declaration of Point Array
                /*
                 * box[0] = Xmin
                 * box[1] = Ymin
                 * box[2] = Zmin
                 * box[3] = Xmax
                 * box[4] = Ymax
                 * box[5] = Zmax
                 */
                double[] box = new double[6]; // Declaration of box Array
                double radius; // Declaration of Radius
                double rad_date; // Declaration of Radial Data
                int nor_dir; // Declaration of Normal Direction of Face
                // Ufunc to get the Face Data
                theUFSession.Modl.AskFaceData(face.Tag, out type, point, direction, box, out radius, out rad_date, out nor_dir);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static bool VolumeOfFluidLogic1(out double volumeOfFluid)
        {
            volumeOfFluid = 0;
            try
            {
                workPart = theSession.Parts.Work;

                Face inFace = null, outFace = null;
                foreach (Body body in workPart.Bodies)
                {
                    foreach (Face face in body.GetFaces())
                    {
                        if (face.SolidFaceType == Face.FaceType.Planar) // Condition for finding Planar faces of body
                        {
                            inFace = face;
                            //double[] point = new double[3]; // To store the out Variable of GetFaceData function
                            //double[] FaceDetails = new double[3]; // To store the out Variable of GetFaceData function
                            //FindFaceDirection(face, out FaceDetails, out point); // GetFaceData function
                            //// Condition for Required In Face
                            //if (Math.Round(FaceDetails[0]) == 1 || Math.Round(FaceDetails[1]) == 1 || Math.Round(FaceDetails[2]) == 1)
                            //{
                            //    outFace = face;
                            //}
                            //// Condition for Required Out Face
                            //if (Math.Round(FaceDetails[0]) == -1|| Math.Round(FaceDetails[1]) == -1|| Math.Round(FaceDetails[2]) == -1) 
                            //{
                            //    inFace = face;
                            //}
                        }
                    }
                }

                Edge inEdge = null, outEdge = null;
                //GetFaceEdge(outFace, out outEdge);
                GetFaceEdge(inFace, out inEdge);
                List<Edge> filterEdges = new List<Edge>();
                filterEdges.Add(inEdge);
                //filterEdges.Add(outEdge);

                Face[] inFaces = inEdge.GetFaces();
                //Face [] outFaces = outEdge.GetFaces();

                List<Face> outerFaces = new List<Face>();
                outerFaces.AddRange(inFaces);
                //outerFaces.AddRange(outFaces);

                Edge reqEdge;
                bool checker = true;
                do
                {
                    RequiredEdgesFaces(outerFaces, filterEdges, out reqEdge, out Face reqFace);
                    filterEdges.Add(reqEdge);
                    outerFaces.Add(reqFace);

                    if (reqFace.SolidFaceType == Face.FaceType.Planar)
                        checker = false;
                    //if (reqEdge.Tag == outEdge.Tag)
                    //    checker = false;

                } while (checker);

                NXOpen.Session.UndoMarkId markId1;
                markId1 = theSession.SetUndoMark(NXOpen.Session.MarkVisibility.Invisible, "DeleteFaces");

                DeleteFaces(outerFaces, out NXOpen.Features.Feature deleteFaceFeature);

                //theSession.SetUndoMarkName(markId1, "DeleteFaces");
                
                foreach (Body bodies in workPart.Bodies)
                {
                    MeasureManager measureManager = workPart.MeasureManager;
                    IBody[] iBody = { bodies };
                    Unit[] volumeUnits = new Unit[1];
                    volumeUnits[0] = workPart.UnitCollection.GetBase("Volume");
                    MeasureBodies measureBodies = measureManager.NewMassProperties(volumeUnits, 0.99, iBody);

                    volumeOfFluid = measureBodies.Volume;
                }

                // Code to delete the Delete Face Feature 

                bool notifyOnDelete1;
                notifyOnDelete1 = theSession.Preferences.Modeling.NotifyOnDelete;

                theSession.UpdateManager.ClearErrorList();

                NXOpen.TaggedObject[] objects1 = new NXOpen.TaggedObject[1];
                // Add the deleteFaceFeature to Object array
                objects1[0] = deleteFaceFeature;
                int nErrs1;
                nErrs1 = theSession.UpdateManager.AddObjectsToDeleteList(objects1);

                // Update the Delete ID using markID
                int nErrs2;
                nErrs2 = theSession.UpdateManager.DoUpdate(markId1);

                // End of the Delect code


                return true;
            }
            catch (Exception ex)
            {
                return false;
                //throw;
            }
        }

        /// <summary>
        /// To Load All Child Part of the Assembly Fully
        /// </summary>
        /// <param name="RootComponent">Root Component of Assembly</param>
        private static void LoadAssemblyFull(Component RootComponent)
        {
            // Check weather Assembly file is open or not
            if (RootComponent != null)
            {
                // Looping through the child components of the rootComponents
                foreach (Component component in RootComponent.GetChildren())
                {
                    // Get the Part of the RootComponent
                    Part partLoadStatusCheck = RootComponent.Prototype.OwningPart as Part;
                    // Chack for IsFullyLoaded or Not
                    if (partLoadStatusCheck.IsFullyLoaded != true)
                    {
                        // Laod the Part Fully
                        component.Prototype.OwningPart.LoadFully();                        
                    }
                }
            }
        }
        /// <summary>
        /// To Open Child Part of an Assembly in New Window
        /// </summary>
        /// <param name="childPart">Pass the Child Part of an Assembly</param>
        private static void OpenAssemblyChildFile(Part childPart)
        {
            // Declaration of partLoadStatus and status
            PartLoadStatus partLoadStatus;
            PartCollection.SdpsStatus status;

            // Code to open Child Part of an Assembly in New Window
            status = theSession.Parts.SetActiveDisplay(childPart, NXOpen.DisplayPartOption.AllowAdditional, NXOpen.PartDisplayPartWorkPartOption.UseLast, out partLoadStatus);

            workPart = theSession.Parts.Work; 
            displayPart = theSession.Parts.Display;

            // Dispose the partLoadStatus variable
            partLoadStatus.Dispose();

            //theSession.ApplicationSwitchImmediate("UG_APP_MODELING");
        }
        /// <summary>
        /// To Close the open child part of an Assembly and back to Main Assembly
        /// </summary>
        /// <param name="rootComponentPart">Main Assembly Root component Part</param>
        private static void CloseAssemblyChildFile(Part rootComponentPart)
        {
            // Code to close the open child part of an Assembly and back to Main Assembly
            workPart.Undisplay();

            workPart = null;
            displayPart = null;
            
            NXOpen.PartLoadStatus partLoadStatus2;
            NXOpen.PartCollection.SdpsStatus status2;
            status2 = theSession.Parts.SetActiveDisplay(rootComponentPart, NXOpen.DisplayPartOption.AllowAdditional, NXOpen.PartDisplayPartWorkPartOption.UseLast, out partLoadStatus2);

            workPart = theSession.Parts.Work; 
            displayPart = theSession.Parts.Display; 
            partLoadStatus2.Dispose();

        }
        
    }
}
