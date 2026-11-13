import type { TaskEnvironment } from "../../../shared/contracts";

type EnvironmentPickerProps = {
  value: TaskEnvironment;
  onChange: (value: TaskEnvironment) => void;
};

const environments: Array<{ id: TaskEnvironment; label: string; description: string; available: boolean }> = [
  { id: "local", label: "Local", description: "Edit the selected project directly", available: true },
  { id: "worktree", label: "Worktree", description: "Isolated Git workspace · coming soon", available: false },
];

export function EnvironmentPicker({ value, onChange }: EnvironmentPickerProps) {
  return <fieldset className="environment-picker">
    <legend>Run this task in</legend>
    <div className="environment-options">
      {environments.map((environment) => <label className={`environment-option ${value === environment.id ? "is-selected" : ""} ${!environment.available ? "is-unavailable" : ""}`} key={environment.id}>
        <input type="radio" name="task-environment" value={environment.id} checked={value === environment.id} disabled={!environment.available} onChange={() => onChange(environment.id)} />
        <span className="environment-option-copy"><strong>{environment.label}</strong><small>{environment.description}</small></span>
        {!environment.available && <span className="environment-status">Unavailable</span>}
      </label>)}
    </div>
    <p className="environment-note">Local is the only configured execution environment. Selecting it keeps the task plan bounded to this project.</p>
  </fieldset>;
}
