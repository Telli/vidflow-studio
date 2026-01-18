import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../ui/dialog";
import { Button } from "../ui/button";
import { Textarea } from "../ui/textarea";
import { Checkbox } from "../ui/checkbox";
import { Label } from "../ui/label";
import { AlertTriangle } from "lucide-react";
import { useState } from "react";

interface RevisionModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (feedback: string) => void;
}

export function RevisionModal({ open, onOpenChange, onSubmit }: RevisionModalProps) {
  const [feedback, setFeedback] = useState("");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-zinc-900 border-zinc-800 text-zinc-100 sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Request Revision</DialogTitle>
          <DialogDescription className="text-zinc-500">
            Provide feedback for the AI agents. The scene status will revert to Draft.
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-6 py-4">
          <div className="space-y-3">
             <Label className="text-zinc-300">Feedback</Label>
             <Textarea 
               placeholder="E.g. The pacing feels too slow in the second half. Needs more tension." 
               className="bg-zinc-950 border-zinc-800 text-zinc-200 min-h-[100px]"
               value={feedback}
               onChange={(e) => setFeedback(e.target.value)}
             />
          </div>

          <div className="space-y-3">
             <Label className="text-zinc-300">Issues to Address</Label>
             <div className="space-y-2">
                <div className="flex items-center space-x-2">
                   <Checkbox id="narrative" className="border-zinc-600 data-[state=checked]:bg-amber-600 data-[state=checked]:text-white" />
                   <Label htmlFor="narrative" className="text-zinc-400 font-normal">Narrative / Story Structure</Label>
                </div>
                <div className="flex items-center space-x-2">
                   <Checkbox id="pacing" className="border-zinc-600 data-[state=checked]:bg-amber-600 data-[state=checked]:text-white" />
                   <Label htmlFor="pacing" className="text-zinc-400 font-normal">Pacing / Timing</Label>
                </div>
                <div className="flex items-center space-x-2">
                   <Checkbox id="visual" className="border-zinc-600 data-[state=checked]:bg-amber-600 data-[state=checked]:text-white" />
                   <Label htmlFor="visual" className="text-zinc-400 font-normal">Visual Style / Cinematography</Label>
                </div>
             </div>
          </div>

          <div className="bg-amber-900/10 border border-amber-900/30 p-3 rounded flex items-start gap-3">
             <AlertTriangle className="w-5 h-5 text-amber-600 flex-shrink-0" />
             <p className="text-xs text-amber-500">
                This will trigger a new round of agent proposals and discard unapproved changes.
             </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={() => onOpenChange(false)} className="text-zinc-400 hover:text-zinc-200">Cancel</Button>
          <Button onClick={() => onSubmit(feedback)} className="bg-amber-600 hover:bg-amber-700 text-white">Send to Agents</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
