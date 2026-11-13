import { useEffect, useRef, type ReactNode } from "react";

const focusableSelector = "button:not([disabled]), input:not([disabled]), textarea:not([disabled]), select:not([disabled]), a[href], [tabindex]:not([tabindex=\"-1\"])";

type ModalDialogProps = {
  open: boolean;
  titleId: string;
  onClose: () => void;
  children: ReactNode;
  initialFocusRef?: { current: HTMLElement | null };
  className?: string;
};

function focusableElements(dialog: HTMLDialogElement) {
  return Array.from(dialog.querySelectorAll<HTMLElement>(focusableSelector)).filter((element) => element.offsetParent !== null);
}

export function ModalDialog({ open, titleId, onClose, children, initialFocusRef, className = "" }: ModalDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => { onCloseRef.current = onClose; }, [onClose]);
  useEffect(() => {
    if (!open) {
      returnFocusRef.current?.focus();
      returnFocusRef.current = null;
      return;
    }
    returnFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const focusFrame = window.requestAnimationFrame(() => {
      const first = initialFocusRef?.current ?? focusableElements(dialogRef.current!).at(0);
      first?.focus();
    });
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab") return;
      const dialog = dialogRef.current;
      if (!dialog) return;
      const elements = focusableElements(dialog);
      if (!elements.length) { event.preventDefault(); return; }
      const first = elements[0];
      const last = elements[elements.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => { window.cancelAnimationFrame(focusFrame); document.removeEventListener("keydown", onKeyDown); };
  }, [initialFocusRef, open]);

  if (!open) return null;
  return <dialog ref={dialogRef} className={`${className} ui-dialog`.trim()} open aria-modal="true" aria-labelledby={titleId}>{children}</dialog>;
}
