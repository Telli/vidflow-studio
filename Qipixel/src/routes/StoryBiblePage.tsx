import { useState, useEffect } from "react";
import {
  Users,
  MapPin,
  Book,
  Plus,
  Search,
  MoreVertical,
  User,
  Image as ImageIcon,
} from "lucide-react";
import { Button } from "../components/ui/button";
import { Input } from "../components/ui/input";
import { ScrollArea } from "../components/ui/scroll-area";
import { Separator } from "../components/ui/separator";
import { Badge } from "../components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { cn } from "../components/ui/utils";
import { useProject } from "../components/context/ProjectContext";
import { vidflowApi, CharacterDto, StoryBibleResponse, CharacterRelationshipDto } from "../api/vidflow";
import { LoadingSpinner } from "../components/common/LoadingSpinner";

// Types for story bible items
type CharacterItem = {
  id: string;
  name: string;
  role: string;
  archetype: string;
  age: string;
  description: string;
  backstory: string;
  traits: string[];
  relationships: CharacterRelationshipDto[];
};

type LocationItem = {
  id: string;
  name: string;
  type: string;
  description: string;
  atmosphere: string;
  keyScenes: string[];
  images: string[];
};

type LoreItem = {
  id: string;
  title: string;
  category: string;
  content: string;
};

type SelectedItem = CharacterItem | LocationItem | LoreItem | null;

// Fallback mock data (used when backend has no data)
const FALLBACK_CHARACTERS = [
  {
    id: "c1",
    name: "Elias Thorne",
    role: "Protagonist",
    archetype: "The Hermit / The Seeker",
    age: "54",
    description: "Weathered by salt and isolation. A man who prefers the company of static to people.",
    traits: ["Obsessive", "Resourceful", "Paranoid"],
    backstory: "Former naval communications officer. Took the lighthouse job 12 years ago after his wife's disappearance. He believes she is still out there, somewhere in the frequencies.",
    relationships: [
      { name: "The Visitor", type: "Antagonist/Guide", note: "Fear turning into reverence" }
    ]
  },
  {
    id: "c2",
    name: "The Visitor",
    role: "Antagonist",
    archetype: "The Herald",
    age: "Unknown",
    description: "A being of pure light and sound. Formless but imposing.",
    traits: ["Silent", "Omnipresent", "Benevolent?"],
    backstory: "Origin unknown. Arrived following the signal.",
    relationships: [
      { name: "Elias", type: "Subject", note: "Observing him" }
    ]
  }
];

const LOCATIONS = [
  {
    id: "l1",
    name: "The Lighthouse",
    type: "Primary Setting",
    description: "A crumbling Victorian-era structure on the edge of a jagged cliff. The paint is peeling, and the lens mechanism grinds.",
    atmosphere: "Claustrophobic, Damp, Ancient",
    keyScenes: ["Scene 1: The Routine", "Scene 2: The Anomaly"],
    images: ["figma:asset/lighthouse_ext.jpg"]
  },
  {
    id: "l2",
    name: "Radio Room",
    type: "Interior",
    description: "Filled with outdated tech. Walls covered in charts and star maps. The heart of Elias's obsession.",
    atmosphere: "Electric, Cluttered, Manic",
    keyScenes: ["Scene 2: The Anomaly", "Scene 3: Deciphering"],
    images: []
  }
];

const LORE = [
  {
    id: "lo1",
    title: "The Signal",
    category: "Phenomenon",
    content: "A rhythmic static that shouldn't exist. It broadcasts on a frequency usually reserved for dead space. It repeats every 14 hours, 12 minutes."
  },
  {
    id: "lo2",
    title: "The Keepers",
    category: "History",
    content: "Legend says the previous keepers didn't die; they just stopped being seen. The townspeople believe the lighthouse takes them."
  }
];

export function StoryBiblePage() {
  const { project } = useProject();
  const [activeSection, setActiveSection] = useState<"characters" | "locations" | "lore">("characters");
  const [selectedItem, setSelectedItem] = useState<SelectedItem>(null);
  const [characters, setCharacters] = useState<CharacterItem[]>(FALLBACK_CHARACTERS);
  const [storyBible, setStoryBible] = useState<StoryBibleResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);
        const [charsData, bibleData] = await Promise.allSettled([
          vidflowApi.getCharacters(project.id),
          vidflowApi.getStoryBible(project.id)
        ]);

        if (charsData.status === "fulfilled" && charsData.value.length > 0) {
          setCharacters(charsData.value);
          setSelectedItem(charsData.value[0]);
        } else {
          setCharacters(FALLBACK_CHARACTERS);
          setSelectedItem(FALLBACK_CHARACTERS[0]);
        }

        if (bibleData.status === "fulfilled") {
          setStoryBible(bibleData.value);
        }
      } catch (err) {
        console.error("Failed to load story bible data:", err);
        setSelectedItem(FALLBACK_CHARACTERS[0]);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, [project.id]);

  // Helper to change section and reset selection
  const handleSectionChange = (section: "characters" | "locations" | "lore") => {
    setActiveSection(section);
    if (section === "characters") setSelectedItem(characters[0] ?? null);
    if (section === "locations") setSelectedItem(LOCATIONS[0] ?? null);
    if (section === "lore") setSelectedItem(LORE[0] ?? null);
  };

  return (
    <div className="flex h-full bg-zinc-950 animate-in fade-in duration-500">
      
      {/* Left Sidebar: Navigation & List */}
      <div className="w-80 border-r border-zinc-800 flex flex-col bg-zinc-950">
        <div className="p-4 border-b border-zinc-800">
          <h1 className="text-xl font-bold text-zinc-100 mb-4 px-2">Story Bible</h1>
          <div className="flex gap-1 mb-4">
             <Button 
                variant={activeSection === "characters" ? "secondary" : "ghost"} 
                size="sm" 
                className="flex-1 text-xs"
                onClick={() => handleSectionChange("characters")}
             >
                <Users className="w-3 h-3 mr-2" /> Characters
             </Button>
             <Button 
                variant={activeSection === "locations" ? "secondary" : "ghost"} 
                size="sm" 
                className="flex-1 text-xs"
                onClick={() => handleSectionChange("locations")}
             >
                <MapPin className="w-3 h-3 mr-2" /> Locations
             </Button>
             <Button 
                variant={activeSection === "lore" ? "secondary" : "ghost"} 
                size="sm" 
                className="flex-1 text-xs"
                onClick={() => handleSectionChange("lore")}
             >
                <Book className="w-3 h-3 mr-2" /> Lore
             </Button>
          </div>
          <div className="relative">
             <Search className="w-4 h-4 absolute left-3 top-2.5 text-zinc-500" />
             <Input placeholder="Search..." className="pl-9 bg-zinc-900 border-zinc-800 h-9" />
          </div>
        </div>

        <ScrollArea className="flex-1">
           <div className="p-2 space-y-1">
              {loading && (
                 <div className="py-8">
                    <LoadingSpinner message="Loading..." />
                 </div>
              )}
              {!loading && activeSection === "characters" && characters.map(char => (
                 <div 
                    key={char.id}
                    onClick={() => setSelectedItem(char)}
                    className={cn(
                        "p-3 rounded-lg cursor-pointer flex items-center gap-3 transition-colors",
                        selectedItem?.id === char.id ? "bg-zinc-800" : "hover:bg-zinc-900"
                    )}
                 >
                    <div className="w-10 h-10 rounded-full bg-zinc-700 flex items-center justify-center flex-shrink-0">
                       <User className="w-5 h-5 text-zinc-400" />
                    </div>
                    <div className="overflow-hidden">
                       <h4 className="text-sm font-medium text-zinc-200 truncate">{char.name}</h4>
                       <p className="text-xs text-zinc-500 truncate">{char.role}</p>
                    </div>
                 </div>
              ))}

              {!loading && activeSection === "locations" && LOCATIONS.map(loc => (
                 <div 
                    key={loc.id}
                    onClick={() => setSelectedItem(loc)}
                    className={cn(
                        "p-3 rounded-lg cursor-pointer flex items-center gap-3 transition-colors",
                        selectedItem?.id === loc.id ? "bg-zinc-800" : "hover:bg-zinc-900"
                    )}
                 >
                    <div className="w-10 h-10 rounded bg-zinc-700 flex items-center justify-center flex-shrink-0">
                       <MapPin className="w-5 h-5 text-zinc-400" />
                    </div>
                    <div className="overflow-hidden">
                       <h4 className="text-sm font-medium text-zinc-200 truncate">{loc.name}</h4>
                       <p className="text-xs text-zinc-500 truncate">{loc.type}</p>
                    </div>
                 </div>
              ))}

              {!loading && activeSection === "lore" && LORE.map(item => (
                 <div 
                    key={item.id}
                    onClick={() => setSelectedItem(item)}
                    className={cn(
                        "p-3 rounded-lg cursor-pointer flex flex-col gap-1 transition-colors",
                        selectedItem?.id === item.id ? "bg-zinc-800" : "hover:bg-zinc-900"
                    )}
                 >
                    <h4 className="text-sm font-medium text-zinc-200 truncate">{item.title}</h4>
                    <Badge variant="outline" className="w-fit text-[10px] py-0 border-zinc-700 text-zinc-500">{item.category}</Badge>
                 </div>
              ))}

              <Button variant="ghost" className="w-full mt-4 border border-dashed border-zinc-800 text-zinc-500 hover:text-zinc-300">
                 <Plus className="w-4 h-4 mr-2" /> Add Item
              </Button>
           </div>
        </ScrollArea>
      </div>

      {/* Main Content: Details */}
      <div className="flex-1 flex flex-col bg-zinc-950/50">
        {selectedItem ? (
           <div className="flex-1 flex flex-col">
              {/* Header */}
              <div className="h-20 border-b border-zinc-800 flex items-center justify-between px-8 bg-zinc-950">
                 <div>
                    <h2 className="text-2xl font-bold text-zinc-100">{selectedItem.name || selectedItem.title}</h2>
                    <p className="text-zinc-500 text-sm">
                        {activeSection === "characters" && selectedItem.archetype}
                        {activeSection === "locations" && selectedItem.type}
                        {activeSection === "lore" && selectedItem.category}
                    </p>
                 </div>
                 <Button variant="outline" className="border-zinc-700 text-zinc-400 hover:text-zinc-100">
                    <MoreVertical className="w-4 h-4" />
                 </Button>
              </div>
              
              <ScrollArea className="flex-1 p-8">
                 <div className="max-w-3xl space-y-8">
                    
                    {/* Character Details */}
                    {activeSection === "characters" && (
                        <>
                           <div className="grid grid-cols-3 gap-4">
                              <Card className="bg-zinc-900 border-zinc-800">
                                 <CardHeader className="pb-2"><CardTitle className="text-xs text-zinc-500 uppercase">Age</CardTitle></CardHeader>
                                 <CardContent><p className="text-lg text-zinc-200">{selectedItem.age}</p></CardContent>
                              </Card>
                              <Card className="bg-zinc-900 border-zinc-800 col-span-2">
                                 <CardHeader className="pb-2"><CardTitle className="text-xs text-zinc-500 uppercase">Role</CardTitle></CardHeader>
                                 <CardContent><p className="text-lg text-zinc-200">{selectedItem.role}</p></CardContent>
                              </Card>
                           </div>

                           <div>
                              <h3 className="text-lg font-semibold text-zinc-200 mb-3">Description</h3>
                              <p className="text-zinc-400 leading-relaxed">{selectedItem.description}</p>
                           </div>

                           <div className="flex gap-2">
                              {selectedItem.traits.map((trait: string) => (
                                 <Badge key={trait} className="bg-zinc-800 hover:bg-zinc-700 text-zinc-300 border-zinc-700 px-3 py-1 text-sm">{trait}</Badge>
                              ))}
                           </div>

                           <Separator className="bg-zinc-800" />

                           <div>
                              <h3 className="text-lg font-semibold text-zinc-200 mb-3">Backstory</h3>
                              <p className="text-zinc-400 leading-relaxed">{selectedItem.backstory}</p>
                           </div>

                           <div>
                              <h3 className="text-lg font-semibold text-zinc-200 mb-3">Relationships</h3>
                              <div className="grid gap-3">
                                 {(selectedItem as CharacterItem).relationships.map((rel, i) => (
                                    <div key={i} className="flex items-center justify-between p-4 bg-zinc-900 rounded-lg border border-zinc-800">
                                       <div>
                                          <span className="font-medium text-zinc-200">{rel.name}</span>
                                          <span className="text-zinc-500 mx-2">•</span>
                                          <span className="text-zinc-400 text-sm">{rel.type}</span>
                                       </div>
                                       <span className="text-sm text-zinc-500 italic">"{rel.note}"</span>
                                    </div>
                                 ))}
                              </div>
                           </div>
                        </>
                    )}

                    {/* Location Details */}
                    {activeSection === "locations" && (
                        <>
                            <div className="aspect-video bg-zinc-900 rounded-xl border border-zinc-800 flex items-center justify-center relative overflow-hidden group">
                               {selectedItem.images.length > 0 ? (
                                   // In a real app we'd load the image, here just a placeholder
                                   <div className="text-zinc-600">Image: {selectedItem.images[0]}</div>
                               ) : (
                                   <div className="flex flex-col items-center text-zinc-600">
                                       <ImageIcon className="w-12 h-12 mb-2 opacity-20" />
                                       <span className="text-sm">No Concept Art</span>
                                   </div>
                               )}
                               <Button variant="secondary" className="absolute bottom-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity">
                                  Generate Visual
                               </Button>
                            </div>

                            <div>
                               <h3 className="text-lg font-semibold text-zinc-200 mb-3">Description</h3>
                               <p className="text-zinc-400 leading-relaxed">{selectedItem.description}</p>
                            </div>

                            <div>
                               <h3 className="text-lg font-semibold text-zinc-200 mb-3">Atmosphere</h3>
                               <p className="text-amber-500 font-medium">{selectedItem.atmosphere}</p>
                            </div>

                            <Separator className="bg-zinc-800" />

                            <div>
                               <h3 className="text-lg font-semibold text-zinc-200 mb-3">Key Scenes</h3>
                               <div className="flex flex-col gap-2">
                                  {selectedItem.keyScenes.map((scene: string, i: number) => (
                                     <Button key={i} variant="outline" className="justify-start border-zinc-800 text-zinc-400 hover:text-zinc-200 h-auto py-3">
                                        <MapPin className="w-4 h-4 mr-3 text-zinc-600" />
                                        {scene}
                                     </Button>
                                  ))}
                               </div>
                            </div>
                        </>
                    )}

                    {/* Lore Details */}
                    {activeSection === "lore" && (
                        <>
                           <Card className="bg-zinc-900 border-zinc-800">
                              <CardContent className="p-8">
                                 <p className="text-lg text-zinc-300 font-serif leading-loose">
                                    {selectedItem.content}
                                 </p>
                              </CardContent>
                           </Card>
                           
                           <div className="flex gap-4">
                              <Button variant="outline" className="border-zinc-700 text-zinc-400">
                                 Link to Scene
                              </Button>
                              <Button variant="outline" className="border-zinc-700 text-zinc-400">
                                 Add Footnote
                              </Button>
                           </div>
                        </>
                    )}
                 </div>
              </ScrollArea>
           </div>
        ) : (
           <div className="flex-1 flex flex-col items-center justify-center text-zinc-600">
              <Book className="w-16 h-16 mb-4 opacity-20" />
              <p>Select an item from the bible to view details.</p>
           </div>
        )}
      </div>
    </div>
  );
}
