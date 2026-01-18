import { useState, useEffect } from "react";
import { Button } from "../../ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../../ui/card";
import { Image, Loader2, Plus, RefreshCw } from "lucide-react";
import { vidflowApi, type AssetDto } from "../../../api/vidflow";
import { useProject } from "../../context/ProjectContext";

interface AssetsPanelProps {
  sceneId?: string;
  shotId?: string;
}

export function AssetsPanel({ sceneId, shotId }: AssetsPanelProps) {
  const { project } = useProject();
  const [assets, setAssets] = useState<AssetDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);

  async function loadAssets() {
    try {
      setIsLoading(true);
      const all = await vidflowApi.listAssets(project.id);
      let filtered = all;
      if (sceneId) {
        filtered = all.filter(a => a.sceneId === sceneId);
      }
      if (shotId) {
        filtered = filtered.filter(a => a.shotId === shotId);
      }
      setAssets(filtered);
    } catch (err) {
      console.error("Failed to load assets:", err);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    loadAssets();
  }, [project.id, sceneId, shotId]);

  async function handleCreateAsset() {
    if (!sceneId || !shotId) return;
    
    setIsCreating(true);
    try {
      await vidflowApi.createStoryboardAsset(project.id, sceneId, shotId);
      await loadAssets();
    } catch (err) {
      console.error("Failed to create asset:", err);
    } finally {
      setIsCreating(false);
    }
  }

  function getStatusColor(status: string) {
    switch (status) {
      case "Ready": return "bg-green-500/20 text-green-400 border-green-500/30";
      case "Generating": return "bg-amber-500/20 text-amber-400 border-amber-500/30";
      case "Failed": return "bg-red-500/20 text-red-400 border-red-500/30";
      default: return "bg-zinc-500/20 text-zinc-400 border-zinc-500/30";
    }
  }

  return (
    <Card className="bg-zinc-900 border-zinc-800">
      <CardHeader className="flex flex-row items-center justify-between pb-3">
        <CardTitle className="text-sm font-medium text-zinc-300">Assets</CardTitle>
        <div className="flex gap-2">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-zinc-500 hover:text-zinc-300"
            onClick={loadAssets}
            disabled={isLoading}
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? "animate-spin" : ""}`} />
          </Button>
          {sceneId && shotId && (
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-zinc-500 hover:text-zinc-300"
              onClick={handleCreateAsset}
              disabled={isCreating}
            >
              {isCreating ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Plus className="w-3.5 h-3.5" />
              )}
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-2">
        {isLoading ? (
          <div className="py-4 text-center">
            <Loader2 className="w-5 h-5 mx-auto text-zinc-500 animate-spin" />
          </div>
        ) : assets.length === 0 ? (
          <div className="py-4 text-center text-zinc-500 text-sm">
            <Image className="w-8 h-8 mx-auto mb-2 opacity-30" />
            <p>No assets yet</p>
            {sceneId && shotId && (
              <Button
                variant="outline"
                size="sm"
                className="mt-3 border-zinc-700 text-zinc-400 hover:text-zinc-200"
                onClick={handleCreateAsset}
                disabled={isCreating}
              >
                {isCreating ? "Generating..." : "Generate Storyboard"}
              </Button>
            )}
          </div>
        ) : (
          assets.map((asset) => (
            <div
              key={asset.id}
              className="flex items-center gap-3 p-2 rounded bg-zinc-950/50 border border-zinc-800"
            >
              <div className="w-12 h-12 bg-zinc-800 rounded flex items-center justify-center flex-shrink-0">
                {asset.url ? (
                  <img
                    src={asset.url}
                    alt={asset.name}
                    className="w-full h-full object-cover rounded"
                  />
                ) : (
                  <Image className="w-5 h-5 text-zinc-600" />
                )}
              </div>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium text-zinc-300 truncate">
                  {asset.name}
                </div>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="text-xs text-zinc-500">{asset.type}</span>
                  <span
                    className={`text-xs px-1.5 py-0.5 rounded border ${getStatusColor(asset.status)}`}
                  >
                    {asset.status}
                  </span>
                </div>
              </div>
            </div>
          ))
        )}
      </CardContent>
    </Card>
  );
}
