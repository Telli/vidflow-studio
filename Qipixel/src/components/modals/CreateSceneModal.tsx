import { useEffect, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../ui/dialog";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Textarea } from "../ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";
import { Loader2 } from "lucide-react";
import { vidflowApi, CreateSceneRequest } from "../../api/vidflow";
import { useToast } from "../ui/use-toast";

interface CreateSceneModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
  nextSceneNumber: string;
  onSceneCreated: () => void | Promise<void>;
}

export function CreateSceneModal({
  open,
  onOpenChange,
  projectId,
  nextSceneNumber,
  onSceneCreated,
}: CreateSceneModalProps) {
  const { toast } = useToast();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateSceneRequest>({
    number: nextSceneNumber,
    title: "",
    narrativeGoal: "",
    emotionalBeat: "",
    location: "",
    timeOfDay: "Day",
    runtimeTargetSeconds: 120,
    characterNames: [],
  });

  useEffect(() => {
    if (!open) return;
    setFormData({
      number: nextSceneNumber,
      title: "",
      narrativeGoal: "",
      emotionalBeat: "",
      location: "",
      timeOfDay: "Day",
      runtimeTargetSeconds: 120,
      characterNames: [],
    });
  }, [open, nextSceneNumber]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!formData.title.trim()) {
      toast({ title: "Title required", description: "Please enter a scene title.", variant: "destructive" });
      return;
    }

    setIsSubmitting(true);
    try {
      await vidflowApi.createScene(projectId, formData);
      toast({ title: "Scene created", description: `Scene ${formData.number}: ${formData.title} created successfully.` });
      await Promise.resolve(onSceneCreated());
      onOpenChange(false);
      // Reset form
      setFormData({
        number: nextSceneNumber,
        title: "",
        narrativeGoal: "",
        emotionalBeat: "",
        location: "",
        timeOfDay: "Day",
        runtimeTargetSeconds: 120,
        characterNames: [],
      });
    } catch (err) {
      toast({ title: "Failed to create scene", description: String(err), variant: "destructive" });
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-zinc-900 border-zinc-800 text-zinc-100 sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Create New Scene</DialogTitle>
          <DialogDescription className="text-zinc-400">
            Add a new scene to your project. You can edit details later.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-4 gap-4">
            <div className="space-y-2">
              <Label htmlFor="number" className="text-zinc-300">Scene #</Label>
              <Input
                id="number"
                value={formData.number}
                onChange={(e) => setFormData({ ...formData, number: e.target.value })}
                className="bg-zinc-950 border-zinc-800 text-zinc-100"
                placeholder="1"
              />
            </div>
            <div className="col-span-3 space-y-2">
              <Label htmlFor="title" className="text-zinc-300">Title *</Label>
              <Input
                id="title"
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                className="bg-zinc-950 border-zinc-800 text-zinc-100"
                placeholder="The Opening"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="narrativeGoal" className="text-zinc-300">Narrative Goal</Label>
            <Textarea
              id="narrativeGoal"
              value={formData.narrativeGoal}
              onChange={(e) => setFormData({ ...formData, narrativeGoal: e.target.value })}
              className="bg-zinc-950 border-zinc-800 text-zinc-100 min-h-[80px]"
              placeholder="What story purpose does this scene serve?"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="emotionalBeat" className="text-zinc-300">Emotional Beat</Label>
            <Input
              id="emotionalBeat"
              value={formData.emotionalBeat}
              onChange={(e) => setFormData({ ...formData, emotionalBeat: e.target.value })}
              className="bg-zinc-950 border-zinc-800 text-zinc-100"
              placeholder="Tension -> Release"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="location" className="text-zinc-300">Location</Label>
              <Input
                id="location"
                value={formData.location}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                className="bg-zinc-950 border-zinc-800 text-zinc-100"
                placeholder="Lighthouse - Kitchen"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="timeOfDay" className="text-zinc-300">Time of Day</Label>
              <Select
                value={formData.timeOfDay}
                onValueChange={(value) => setFormData({ ...formData, timeOfDay: value })}
              >
                <SelectTrigger className="bg-zinc-950 border-zinc-800 text-zinc-100">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100">
                  <SelectItem value="Day">Day</SelectItem>
                  <SelectItem value="Night">Night</SelectItem>
                  <SelectItem value="Dawn">Dawn</SelectItem>
                  <SelectItem value="Dusk">Dusk</SelectItem>
                  <SelectItem value="Continuous">Continuous</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="runtime" className="text-zinc-300">Target Runtime (seconds)</Label>
            <Input
              id="runtime"
              type="number"
              min={10}
              max={600}
              value={formData.runtimeTargetSeconds}
              onChange={(e) => setFormData({ ...formData, runtimeTargetSeconds: parseInt(e.target.value) || 120 })}
              className="bg-zinc-950 border-zinc-800 text-zinc-100 w-32"
            />
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="border-zinc-700 text-zinc-300 hover:bg-zinc-800"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              className="bg-amber-600 hover:bg-amber-700 text-white"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Creating...
                </>
              ) : (
                "Create Scene"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
