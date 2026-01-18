import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "../../ui/card";
import { Progress } from "../../ui/progress";
import { DollarSign, TrendingUp } from "lucide-react";
import { vidflowApi, type ProjectCostsResponse } from "../../../api/vidflow";

interface BudgetCardProps {
  projectId: string;
}

export function BudgetCard({ projectId }: BudgetCardProps) {
  const [costs, setCosts] = useState<ProjectCostsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadCosts() {
      try {
        const data = await vidflowApi.getProjectCosts(projectId);
        setCosts(data);
      } catch (err) {
        console.error("Failed to load costs:", err);
      } finally {
        setIsLoading(false);
      }
    }
    loadCosts();
  }, [projectId]);

  if (isLoading || !costs) {
    return (
      <Card className="bg-zinc-900 border-zinc-800">
        <CardContent className="p-6">
          <div className="animate-pulse space-y-3">
            <div className="h-4 bg-zinc-800 rounded w-1/3"></div>
            <div className="h-8 bg-zinc-800 rounded w-1/2"></div>
            <div className="h-2 bg-zinc-800 rounded w-full"></div>
          </div>
        </CardContent>
      </Card>
    );
  }

  const utilizationColor = costs.budgetUtilizationPercent > 90 
    ? "text-red-400" 
    : costs.budgetUtilizationPercent > 70 
      ? "text-amber-400" 
      : "text-green-400";

  return (
    <Card className="bg-zinc-900 border-zinc-800">
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-zinc-400 flex items-center gap-2">
          <DollarSign className="w-4 h-4" />
          Budget Usage
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-baseline justify-between">
          <div className="text-2xl font-bold text-zinc-100">
            ${costs.currentSpendUsd.toFixed(2)}
          </div>
          <div className="text-sm text-zinc-500">
            of ${costs.budgetCapUsd.toFixed(2)}
          </div>
        </div>
        
        <Progress 
          value={Math.min(costs.budgetUtilizationPercent, 100)} 
          className="h-2 bg-zinc-800"
        />
        
        <div className="flex items-center justify-between text-xs">
          <span className={utilizationColor}>
            {costs.budgetUtilizationPercent.toFixed(1)}% used
          </span>
          <span className="text-zinc-500">
            ${costs.remainingBudget.toFixed(2)} remaining
          </span>
        </div>

        <div className="pt-2 border-t border-zinc-800 space-y-2">
          <div className="flex items-center justify-between text-xs text-zinc-400">
            <span className="flex items-center gap-1">
              <TrendingUp className="w-3 h-3" />
              Total Proposals
            </span>
            <span className="text-zinc-300">{costs.totalProposals}</span>
          </div>
          <div className="flex items-center justify-between text-xs text-zinc-400">
            <span>Tokens Used</span>
            <span className="text-zinc-300">{costs.totalTokensUsed.toLocaleString()}</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
