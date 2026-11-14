import type { TaskEnvironment } from "../../../shared/contracts";

type EnvironmentPickerProps = {
  value: TaskEnvironment;
  onChange: (value: TaskEnvironment) => void;
  hasProject?: boolean;
};

const environments: Array<{ id: TaskEnvironment; label: string; description: string }> = [
  { id: "local", label: "Local", description: "Use the selected project for bounded context" },
  { id: "worktree", label: "Worktree", description: "Create an isolated Git workspace after approval" },
];

export function EnvironmentPicker({ value, onChange, hasProject = false }: EnvironmentPickerProps) {
  return <fieldset className="environment-picker">
    <legend>Run this task in</legend>
    <div className="environment-options">
      {environments.map((environment) => {
        const available = environment.id === "local" || hasProject;
        return <label className={`environment-option ${value === environment.id ? "is-selected" : ""} ${!available ? "is-unavailable" : ""}`} key={environment.id}>
        <input type="radio" name="task-environment" value={environment.id} checked={value === environment.id} disabled={!available} onChange={() => onChange(environment.id)} />
        <span className="environment-option-copy"><strong>{environment.label}</strong><small>{environment.description}</small></span>
        {!available && <span className="environment-status">Select project</span>}
      </label>;
      })}
    </div>
    <p className="environment-note">Worktree requires a selected Git project and keeps the isolated branch until you explicitly release it.</p>
  </fieldset>;
}
