import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import App from "./App";
import { PetWindowApp } from "./components/overlays/PetWindowApp";
import "./design-system/tokens.css";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {window.location.hash === "#pet" ? <PetWindowApp /> : <App />}
  </StrictMode>,
);
