import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { errorMessage } from "@/api/errors";
import Alert from "@/components/ui/Alert/Alert";
import Button from "@/components/ui/Button/Button";
import Card from "@/components/ui/Card/Card";
import EmptyState from "@/components/ui/EmptyState/EmptyState";
import {
  CheckboxField,
  SelectField,
  TextField,
} from "@/components/ui/Field/Field";
import Icon from "@/components/ui/Icon/Icon";
import { formatMoney } from "@/shared/format";
import { LIMITS, isRoomValid, newRoom, roomName } from "./draft";
import { ROOM_TYPES, useRoomTemplates, useWorkItems } from "./estimatorApi";
import RoomCard from "./RoomCard";
import { useMeasure } from "./useMeasure";
import WorkPicker from "./WorkPicker";
import styles from "./estimator.module.scss";

/**
 * The room-by-room estimator: a title, "old building", the rooms, and a summary with the total price range that
 * updates as the customer types. `actions` go under the total (save, share…); `notice` above the rooms.
 */
export default function EstimateEditor({ draft, onChange, actions, notice }) {
  const { t, i18n } = useTranslation();
  const lng = i18n.language;
  const templates = useRoomTemplates();
  const { data: workItemList = [] } = useWorkItems();
  const workItems = useMemo(
    () => new Map(workItemList.map((item) => [item.id, item])),
    [workItemList],
  );
  const measure = useMeasure(draft);
  const [newType, setNewType] = useState("Bedroom");
  // The key of the room the work picker adds to, while it's open.
  const [picking, setPicking] = useState(null);

  const setRooms = (rooms) => onChange({ ...draft, rooms });
  const setRoom = (key, room) =>
    setRooms(
      draft.rooms.map((current) => (current.key === key ? room : current)),
    );
  const addRoom = () => {
    const template = templates.data?.find(
      (candidate) => candidate.type === newType,
    );
    setRooms([
      ...draft.rooms,
      newRoom(
        newType,
        roomName(t(`estimator.roomTypes.${newType}`), draft.rooms),
        template,
      ),
    ]);
  };
  const moveRoom = (index, step) => {
    const rooms = [...draft.rooms];
    [rooms[index], rooms[index + step]] = [rooms[index + step], rooms[index]];
    setRooms(rooms);
  };

  const pickingRoom = draft.rooms.find((room) => room.key === picking);
  const total = measure.total;
  const unmeasured = draft.rooms.filter((room) => !isRoomValid(room)).length;

  return (
    <div className={styles.estimator}>
      <div className={styles["estimator__main"]}>
        <Card>
          <div className={styles["estimator__settings"]}>
            <TextField
              label={t("estimator.editor.title")}
              value={draft.title}
              maxLength={LIMITS.title}
              placeholder={t("estimator.editor.defaultTitle")}
              onChange={(event) =>
                onChange({ ...draft, title: event.target.value })
              }
            />
            <CheckboxField
              label={t("estimator.oldBuilding")}
              checked={draft.oldBuilding}
              onChange={(event) =>
                onChange({ ...draft, oldBuilding: event.target.checked })
              }
            />
            <p className={styles["estimator__hint"]}>
              {t("estimator.editor.oldBuildingHint")}
            </p>
          </div>
        </Card>

        {notice}

        {draft.rooms.length === 0 && (
          <EmptyState
            icon="home"
            title={t("estimator.editor.noRooms")}
            description={t("estimator.editor.noRoomsText")}
          />
        )}
        {draft.rooms.map((room, index) => (
          <RoomCard
            key={room.key}
            room={room}
            measured={measure.room(room.key)}
            workItems={workItems}
            first={index === 0}
            last={index === draft.rooms.length - 1}
            onChange={(changed) => setRoom(room.key, changed)}
            onRemove={() =>
              setRooms(
                draft.rooms.filter((current) => current.key !== room.key),
              )
            }
            onMove={(step) => moveRoom(index, step)}
            onAddWork={() => setPicking(room.key)}
          />
        ))}

        <Card>
          <div className={styles["estimator__add-room"]}>
            <SelectField
              label={t("estimator.editor.newRoom")}
              value={newType}
              onChange={(event) => setNewType(event.target.value)}
              options={ROOM_TYPES.map((type) => ({
                value: type,
                label: t(`estimator.roomTypes.${type}`),
              }))}
            />
            <Button
              variant="secondary"
              icon={<Icon name="plus" />}
              disabled={
                draft.rooms.length >= LIMITS.rooms || templates.isLoading
              }
              onClick={addRoom}
            >
              {t("estimator.editor.addRoom")}
            </Button>
          </div>
        </Card>
      </div>

      <aside
        className={styles["estimator__summary"]}
        aria-label={t("estimator.editor.summary")}
      >
        <Card title={t("estimator.editor.summary")}>
          <div
            className={styles["estimate-summary"]}
            aria-live="polite"
            aria-busy={measure.loading}
          >
            {total && total.totalTypical > 0 ? (
              <>
                <p className={styles["quick-estimate__label"]}>
                  {t("estimator.quick.resultLabel")}
                </p>
                <p className={styles["quick-estimate__range"]}>
                  {formatMoney(total.totalMin, lng)} –{" "}
                  {formatMoney(total.totalMax, lng)}
                </p>
                <p className={styles["quick-estimate__typical"]}>
                  {t("estimator.quick.usually", {
                    amount: formatMoney(total.totalTypical, lng),
                  })}
                </p>
              </>
            ) : (
              <p className={styles["estimate-summary__empty"]}>
                {t("estimator.editor.noTotal")}
              </p>
            )}
            {unmeasured > 0 && (
              <p className={styles["estimate-summary__note"]}>
                {t("estimator.editor.unmeasured", { number: unmeasured })}
              </p>
            )}
            {total?.unpricedLines > 0 && (
              <p className={styles["estimate-summary__note"]}>
                {t("estimator.editor.unpriced", {
                  number: total.unpricedLines,
                })}
              </p>
            )}
            {measure.error && (
              <Alert tone="warning" title={errorMessage(t, measure.error)} />
            )}
            <p className={styles["quick-estimate__note"]}>
              {t("estimator.quick.note")}
            </p>
            {actions && (
              <div className={styles["estimate-summary__actions"]}>
                {actions}
              </div>
            )}
          </div>
        </Card>
      </aside>

      <WorkPicker
        open={Boolean(pickingRoom)}
        roomName={pickingRoom?.name ?? ""}
        workItems={workItemList}
        taken={new Set(pickingRoom?.lines.map((line) => line.workItemId) ?? [])}
        onPick={(item) =>
          pickingRoom &&
          setRoom(pickingRoom.key, {
            ...pickingRoom,
            lines: [
              ...pickingRoom.lines,
              { workItemId: item.id, quantity: "", perSquareMeter: null },
            ],
          })
        }
        onClose={() => setPicking(null)}
      />
    </div>
  );
}
