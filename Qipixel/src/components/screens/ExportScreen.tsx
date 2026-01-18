import { useState, useEffect } from "react";
import { Project } from "../data/mock-data";
import { Button } from "../ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../ui/card";
import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Label } from "../ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";
import { Separator } from "../ui/separator";
import { Download, FileVideo, Loader2, AlertCircle } from "lucide-react";
import { vidflowApi, RenderJobDto } from "../../api/vidflow";
import { useToast } from "../ui/use-toast";
import { formatTimeAgo } from "../../utils/formatTime";
import { LoadingSpinner } from "../common/LoadingSpinner";

interface ExportScreenProps {
  project: Project;
}

export function ExportScreen({ project }: ExportScreenProps) {
  const [renderJobs, setRenderJobs] = useState<RenderJobDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [renderFormat, setRenderFormat] = useState("scene-render");
  const [resolution, setResolution] = useState("1080p");
  const [isRendering, setIsRendering] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    loadRenderJobs();
  }, [project.id]);

  async function loadRenderJobs() {
    try {
      setLoading(true);
      const response = await vidflowApi.listRenderJobs(project.id);
      setRenderJobs(response.jobs);
    } catch (err) {
      console.error("Failed to load render jobs:", err);
    } finally {
      setLoading(false);
    }
  }

  async function handleStartRender() {
    setIsRendering(true);
    try {
      if (renderFormat === "animatic") {
        // For animatic, we need a scene - use first scene for now
        const firstScene = project.scenes[0];
        if (firstScene) {
          await vidflowApi.requestAnimatic(firstScene.id);
          toast({ title: "Animatic render started", description: "Your animatic is being generated." });
        }
      } else {
        // Full render
        await vidflowApi.requestFinalRender(project.id);
        toast({ title: "Final render started", description: "Your film is being rendered." });
      }
      await loadRenderJobs();
    } catch (err) {
      toast({ title: "Render failed", description: String(err), variant: "destructive" });
    } finally {
      setIsRendering(false);
    }
  }

  function handleDownloadPdf() {
    const url = vidflowApi.exportStoryboardPdfUrl(project.id);
    window.open(url, "_blank");
  }

  function getStatusColor(status: string) {
    switch (status) {
      case "Completed": return "text-green-500";
      case "Processing": return "text-amber-500";
      case "Failed": return "text-red-500";
      default: return "text-zinc-500";
    }
  }
  return (
    <div className="p-8 max-w-4xl mx-auto space-y-8 animate-in fade-in duration-500">
      <div>
        <h1 className="text-3xl font-bold text-zinc-100">Render & Export</h1>
        <p className="text-zinc-400 mt-1">Configure output settings for your film deliverables.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
        <div className="space-y-6">
          <Card className="bg-zinc-900 border-zinc-800">
             <CardHeader>
                <CardTitle className="text-zinc-100">Render Configuration</CardTitle>
                <CardDescription className="text-zinc-500">Select format and quality settings.</CardDescription>
             </CardHeader>
             <CardContent className="space-y-6">
                <div className="space-y-3">
                   <Label className="text-zinc-300">Output Format</Label>
                   <RadioGroup value={renderFormat} onValueChange={setRenderFormat} className="grid grid-cols-1 gap-3">
                      <div className="flex items-center space-x-3 p-3 rounded border border-zinc-800 bg-zinc-950/50 hover:border-zinc-700 cursor-pointer">
                         <RadioGroupItem value="animatic" id="animatic" className="border-zinc-600 text-amber-500" />
                         <Label htmlFor="animatic" className="flex-1 cursor-pointer">
                            <div className="font-medium text-zinc-200">Animatic Preview</div>
                            <div className="text-xs text-zinc-500">Low-res stitch of storyboards & placeholders. Fast.</div>
                         </Label>
                      </div>
                      <div className="flex items-center space-x-3 p-3 rounded border border-zinc-800 bg-zinc-950/50 hover:border-zinc-700 cursor-pointer">
                         <RadioGroupItem value="scene-render" id="scene-render" className="border-zinc-600 text-amber-500" />
                         <Label htmlFor="scene-render" className="flex-1 cursor-pointer">
                            <div className="font-medium text-zinc-200">Full Scene Render</div>
                            <div className="text-xs text-zinc-500">High-fidelity render of all approved scenes.</div>
                         </Label>
                      </div>
                   </RadioGroup>
                </div>

                <div className="space-y-3">
                   <Label className="text-zinc-300">Resolution</Label>
                   <Select value={resolution} onValueChange={setResolution}>
                      <SelectTrigger className="bg-zinc-950 border-zinc-800 text-zinc-200">
                         <SelectValue placeholder="Select resolution" />
                      </SelectTrigger>
                      <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-200">
                         <SelectItem value="720p">720p HD</SelectItem>
                         <SelectItem value="1080p">1080p FHD</SelectItem>
                         <SelectItem value="4k">4K UHD</SelectItem>
                      </SelectContent>
                   </Select>
                </div>

                <Separator className="bg-zinc-800" />

                <div className="space-y-3">
                   <Label className="text-zinc-300">Export Storyboard</Label>
                   <Button
                      variant="outline"
                      className="w-full border-zinc-700 text-zinc-300 hover:bg-zinc-800"
                      onClick={handleDownloadPdf}
                   >
                      <Download className="w-4 h-4 mr-2" />
                      Download Storyboard PDF
                   </Button>
                </div>
             </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
           <Card className="bg-zinc-900 border-zinc-800">
              <CardHeader>
                 <CardTitle className="text-zinc-100">Render History</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                 {loading ? (
                    <div className="py-8">
                       <LoadingSpinner message="Loading render jobs..." />
                    </div>
                 ) : renderJobs.length === 0 ? (
                    <div className="text-center py-8 text-zinc-500">
                       <FileVideo className="w-10 h-10 mx-auto mb-2 opacity-30" />
                       <p>No renders yet</p>
                    </div>
                 ) : (
                    renderJobs.slice(0, 5).map((job) => (
                       <div key={job.id} className="flex items-center justify-between p-3 bg-zinc-950/50 rounded border border-zinc-800">
                          <div className="flex items-center gap-3">
                             {job.status === "Processing" ? (
                                <Loader2 className="w-8 h-8 text-amber-500 animate-spin" />
                             ) : job.status === "Failed" ? (
                                <AlertCircle className="w-8 h-8 text-red-500" />
                             ) : (
                                <FileVideo className="w-8 h-8 text-zinc-700" />
                             )}
                             <div>
                                <div className="text-sm font-medium text-zinc-300">
                                   {job.type === "Animatic" ? "Animatic Preview" :
                                    job.type === "Scene" ? "Scene Render" : "Final Render"}
                                </div>
                                <div className="text-xs text-zinc-500 flex items-center gap-2">
                                   <span>{formatTimeAgo(job.createdAt)}</span>
                                   <span className={getStatusColor(job.status)}>• {job.status}</span>
                                   {job.status === "Processing" && (
                                      <span className="text-amber-500">{job.progressPercent}%</span>
                                   )}
                                </div>
                             </div>
                          </div>
                          {job.status === "Completed" && job.artifactPath && (
                             <Button
                                variant="ghost"
                                size="icon"
                                className="text-zinc-400 hover:text-zinc-200"
                                onClick={() => window.open(job.artifactPath, "_blank")}
                             >
                                <Download className="w-4 h-4" />
                             </Button>
                          )}
                       </div>
                    ))
                 )}
              </CardContent>
           </Card>

           <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <h3 className="font-medium text-zinc-200 mb-2">Ready to Render</h3>
              <p className="text-sm text-zinc-400 mb-6">
                 {renderFormat === "animatic"
                    ? "Generate a quick animatic preview from storyboards."
                    : "Render the final high-quality output of your film."}
              </p>
              <Button
                 className="w-full bg-amber-600 hover:bg-amber-700 text-white font-semibold h-12"
                 onClick={handleStartRender}
                 disabled={isRendering}
              >
                 {isRendering ? (
                    <>
                       <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                       Starting Render...
                    </>
                 ) : (
                    "Start Render"
                 )}
              </Button>
           </div>
        </div>
      </div>
    </div>
  );
}
