import { useLocalization } from 'cs2/l10n';
import { Tooltip } from "cs2/ui"
import { trigger } from "cs2/api";
import mod from 'mod.json'
import styles from './itemsStyling.module.scss';
import Checkbox from '../../common/checkbox';

export interface MainPanelCheckboxProps {
  keyName: string;
  isChecked: boolean;
  label: string;
  triggerGroup?: string;
  triggerName: string;
  tooltip?: string;
  onClickOverride?: () => void;
  className?: string;
  disabled?: boolean;
}

export default function MainPanelCheckbox(props: MainPanelCheckboxProps) {
  const { translate } = useLocalization();
  const triggerGroup = props.triggerGroup ?? mod.id;
  const triggerName = `TRIGGER:${props.triggerName}`;
  const wrapperClassName = [props.className, props.disabled ? styles.disabled : ""]
    .filter(Boolean)
    .join(" ");
  const containerClassName = [styles.container, props.disabled ? styles.disabledContainer : ""]
    .filter(Boolean)
    .join(" ");

  const clickHandler = () => {
    if (props.disabled) {
      return;
    }
    if (props.onClickOverride) {
      props.onClickOverride();
      return;
    }
    trigger(triggerGroup, triggerName, JSON.stringify({key: props.keyName, value: props.isChecked ? "false" : "true"}));
  };

  const content = (
    <div
      className={wrapperClassName || undefined}
      aria-disabled={props.disabled}
    >
      <div
        className={containerClassName}
        onClick={clickHandler}
        aria-disabled={props.disabled}
      >
        <div className={styles.titleContainer}>
          <Checkbox isChecked={props.isChecked} />
          <div className={styles.label}>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.label}]`) ?? props.label}</div>
        </div>
      </div>
    </div>
    
  );

  return props.tooltip ? (
    <Tooltip direction="right" tooltip={props.tooltip}>
      {content}
    </Tooltip>
  ) : content;
}
