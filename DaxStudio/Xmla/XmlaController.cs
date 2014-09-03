﻿extern alias ExcelAmo;

using System.Collections.Generic;
using System.Web.Http;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.Net.Http.Headers;
using ExcelAmo::Microsoft.AnalysisServices;
using System.IO;
using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;


namespace DaxStudio.ExcelAddin.Xmla
{
    [RoutePrefix("xmla")]
    public class XmlaController : ApiController
    {

        [HttpPost]
        [Route("")]
        public async Task<HttpResponseMessage> PostRawBufferManual()
        {
            try
            {
                string request = await Request.Content.ReadAsStringAsync();

                var addin = Globals.ThisAddIn;
                var app = addin.Application;
                var wb = app.ActiveWorkbook;

                var loc = wb.FullName;  //@"D:\Data\Presentations\Drop Your DAX\demos\02 DAX filter similar.xlsx";
                var connStr = string.Format("Provider=MSOLAP.5;Persist Security Info=True;Initial Catalog=Microsoft_SQLServer_AnalysisServices;Data Source=$Embedded$;MDX Compatibility=1;Safety Options=2;MDX Missing Member Mode=Error;Subqueries=0;Optimize Response=7;Location={0}", loc);

                // server.execute
                var svr = new ExcelAmo::Microsoft.AnalysisServices.Server();
                svr.Connect(connStr);

                // STEP 1: send the request to server.
                System.IO.TextReader streamWithXmlaRequest = new StringReader(request);
                
                System.Xml.XmlReader xmlaResponseFromServer; // will be used to parse the XML/A response from server

                try
                {
                    xmlaResponseFromServer = svr.SendXmlaRequest(XmlaRequestType.Undefined, streamWithXmlaRequest);
                }
                finally
                {
                    streamWithXmlaRequest.Close();
                }

                // STEP 2: read/parse the XML/A response from server.
                xmlaResponseFromServer.MoveToContent();
                string fullEnvelopeResponseFromServer = xmlaResponseFromServer.ReadOuterXml();

                HttpResponseMessage result;
                try
                {
                    result = new HttpResponseMessage(HttpStatusCode.OK);
                    result.Content = new StringContent(fullEnvelopeResponseFromServer);

                    // TODO - can we stream content rather than buffering it all as a string?
                    //XmlaStream xs = new XmlaStream(xmlaResponseFromServer);
                    //result.Content = new PushStreamContent((strm, http, ctx) => xs.OutputXmlaStream(strm,http, ctx));
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                }
                catch (Exception ex)
                {
                    result = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    result.Content = new StringContent(String.Format("An unexpected error occurred: \n{0}", ex.Message));
                }
                finally
                {
                // STEP 3: close the System.Xml.XmlReader, to release the connection for future use.
                xmlaResponseFromServer.Close();
                }
                return result;
            }
            catch (Exception ex)
            {
                var expResult = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                expResult.Content = new StringContent(String.Format("An unexpected error occurred: \n{0}", ex.Message));
                return expResult;
            }
            //return null;
        }

        
    }

    internal class XmlaStream
    {
        private readonly XmlReader _reader;
        public XmlaStream(XmlReader reader)
        {
            _reader = reader;
        }
        /*
        internal async Task OutputXmlaStream(Stream outputStream, HttpContent httpContent, TransportContext transportContext)
        {
            try
            {
                XmlReader reader = _reader;
                // Create a DB connection
                using (XmlWriter xw = new XmlTextWriter(outputStream, System.Text.Encoding.Unicode))
                {
                    XmlNamespaceManager ns = new XmlNamespaceManager(reader.NameTable);
                    
                    // Read rows asynchronously, put data into buffer and write asynchronously    
                    while ( reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                xw.WriteStartElement(reader.Prefix, reader.LocalName,reader.NamespaceURI);
                                break;
                            case XmlNodeType.Text:
                                xw.WriteString(reader.Value);
                                break;
                            case XmlNodeType.CDATA:
                                xw.WriteCData(reader.Value);
                                break;
                            case XmlNodeType.ProcessingInstruction:
                                xw.WriteProcessingInstruction(reader.Name, reader.Value);
                                break;
                            case XmlNodeType.Comment:
                                xw.WriteComment(reader.Value);
                                break;
                            case XmlNodeType.XmlDeclaration:
                                xw.WriteRaw("<?xml version='1.0'?>");
                                break;
                            case XmlNodeType.Document:
                                break;
                            case XmlNodeType.DocumentType:
                                xw.WriteDocType(reader.Name, reader.Value, null, null);
                                break;
                            case XmlNodeType.EntityReference:
                                xw.WriteEntityRef(reader.Name);
                                break;
                            case XmlNodeType.EndElement:
                                xw.WriteEndElement();
                                break;
                            case XmlNodeType.Attribute:
                                xw.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                                xw.WriteEndAttribute();
                                //xw.WriteAttributes(reader,false);
                                break;
                            default:
                                throw new ArgumentException(string.Format("Unhandled NodeType - {0}", reader.NodeType));
                                
                        }

                    }
                    
                }
            }
            finally
            {
                _reader.Close();
                // Close output stream as we are done
                outputStream.Close();
            }
        }
        */
    }
}