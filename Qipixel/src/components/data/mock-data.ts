import { LucideIcon } from "lucide-react";

export type ProjectStatus = "Ideation" | "Writing" | "Production" | "Locked";
export type SceneStatus = "Draft" | "Review" | "Approved";
export type AgentRole = "Writer" | "Director" | "Cinematographer" | "Editor" | "Producer";
export type ProposalStatus = "Pending" | "Applied" | "Dismissed";

export interface Shot {
  id: string;
  number: number;
  type: string;
  duration: string;
  description: string;
  camera: string;
}

export interface AgentProposal {
  id: string;
  role: AgentRole;
  summary: string;
  rationale: string;
  runtimeImpact: number; // seconds
  diff: string;
  status?: ProposalStatus;
  createdAt?: string;
  tokensUsed?: number;
  costUsd?: number;
}

export interface Scene {
  id: string;
  number: string; // "1" or "1A"
  title: string;
  narrativeGoal: string;
  emotionalBeat: string;
  location: string;
  timeOfDay: string;
  characters: string[];
  status: SceneStatus;
  isLocked?: boolean;
  lockedUntil?: string | null;
  lockedBy?: string | null;
  runtimeEstimate: number; // seconds
  runtimeTarget: number; // seconds
  script: string;
  shots: Shot[];
  proposals: AgentProposal[];
  version: number;
}

export interface Project {
  id: string;
  title: string;
  logline: string;
  runtimeTarget: number; // seconds
  status: ProjectStatus;
  scenes: Scene[];
}

export const MOCK_PROJECT: Project = {
  id: "p1",
  title: "The Last Signal",
  logline: "An isolated lighthouse keeper discovers a pattern in the static that shouldn't exist.",
  runtimeTarget: 900, // 15 minutes
  status: "Production",
  scenes: [
    {
      id: "s1",
      number: "1",
      title: "The Routine",
      narrativeGoal: "Establish the isolation and monotony of the protagonist's life.",
      emotionalBeat: "Solitude -> Disquiet",
      location: "Lighthouse - Kitchen",
      timeOfDay: "Night",
      characters: ["Elias"],
      status: "Approved",
      runtimeEstimate: 120,
      runtimeTarget: 110,
      version: 3,
      script: "ELIAS (50s, weathered) sits at the small table. The soup is cold.\n\nHe stares at the radio in the corner. Silent.\n\nELIAS\nAnother day. Another nothing.",
      shots: [
        { id: "sh1", number: 1, type: "Wide", duration: "5s", description: "Elias alone at table", camera: "Static, 35mm" },
        { id: "sh2", number: 2, type: "CU", duration: "3s", description: "Soup spoon hovering", camera: "Handheld, 85mm" },
      ],
      proposals: []
    },
    {
      id: "s2",
      number: "2",
      title: "The Anomaly",
      narrativeGoal: "Disrupt the routine with the inciting incident.",
      emotionalBeat: "Boredom -> Shock",
      location: "Lighthouse - Radio Room",
      timeOfDay: "Night",
      characters: ["Elias"],
      status: "Review",
      runtimeEstimate: 180,
      runtimeTarget: 160,
      version: 2,
      script: "ELIAS enters the radio room. The static hums—standard white noise.\n\nSuddenly, a RHYTHM cuts through. Three sharp bursts.\n\nELIAS\n(whispering)\nImpossible.",
      shots: [
        { id: "sh3", number: 1, type: "Med", duration: "4s", description: "Elias enters frame", camera: "Dolly in" },
        { id: "sh4", number: 2, type: "ECU", duration: "2s", description: "Radio dial", camera: "Macro" },
      ],
      proposals: [
        {
          id: "p1",
          role: "Director",
          summary: "Increase tension with silence",
          rationale: "The rhythm should be preceded by absolute silence to maximize impact.",
          runtimeImpact: 5,
          diff: "Added 5s beat of silence before the rhythm starts."
        },
        {
          id: "p2",
          role: "Cinematographer",
          summary: "Darker contrast",
          rationale: "The room should feel claustrophobic as the sound enters.",
          runtimeImpact: 0,
          diff: "Lighting notes: Reduce fill, increase key contrast ratio."
        }
      ]
    },
    {
      id: "s3",
      number: "3",
      title: "Deciphering",
      narrativeGoal: "Elias tries to understand the message.",
      emotionalBeat: "Curiosity -> Obsession",
      location: "Lighthouse - Study",
      timeOfDay: "Dawn",
      characters: ["Elias"],
      status: "Draft",
      runtimeEstimate: 240,
      runtimeTarget: 200,
      version: 1,
      script: "Books are sprawled everywhere. Elias is manic, scribbling on a chart.\n\nELIAS\nIt's not Morse. It's... binary? No, something older.",
      shots: [],
      proposals: [
        {
          id: "p3",
          role: "Writer",
          summary: "Sharpen dialogue",
          rationale: "Elias talks too much to himself. Show, don't tell.",
          runtimeImpact: -15,
          diff: "Cut lines 4-8. Replaced with action: Elias furiously underlines a star chart."
        }
      ]
    },
     {
      id: "s4",
      number: "4",
      title: "The Arrival",
      narrativeGoal: "The source of the signal arrives.",
      emotionalBeat: "Fear -> Awe",
      location: "Exterior - Cliffside",
      timeOfDay: "Night",
      characters: ["Elias", "Visitor"],
      status: "Draft",
      runtimeEstimate: 180,
      runtimeTarget: 180,
      version: 1,
      script: "Elias stands at the edge. The wind howls.\n\nA light descends from the clouds. Not a ship. Not a plane.",
      shots: [],
      proposals: []
    }
  ]
};
