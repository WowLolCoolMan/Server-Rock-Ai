import { useEffect, useState } from "react";
import type { PetWindowConfig } from "../../../shared/contracts";
import { PetCompanion } from "./PetCompanion";

export function PetWindowApp() {
  const [config, setConfig] = useState<PetWindowConfig | null>(null);

  useEffect(() => {
    document.title = "Skai Desktop · pet";
    document.documentElement.classList.add("pet-window-document");
    document.body.classList.add("pet-window-document");
    let active = true;
    void window.serverRockAgent.pet.getConfig().then((next) => { if (active) setConfig(next); });
    const unsubscribe = window.serverRockAgent.pet.onConfig((next) => setConfig(next));
    return () => {
      active = false;
      unsubscribe();
      document.documentElement.classList.remove("pet-window-document");
      document.body.classList.remove("pet-window-document");
    };
  }, []);

  if (!config) return null;
  if (!config.visible) return null;
  return <PetCompanion config={config} onHide={() => window.serverRockAgent.pet.hide()} />;
}
