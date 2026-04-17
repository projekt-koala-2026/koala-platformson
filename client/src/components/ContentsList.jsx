import styles from "./ContentsList.module.css";

export const ContentsListTile = ({ children }) => {
    return <div className={styles.tile}>{children}</div>;
};

export const ContentsListBox = ({ children }) => {
    return <div className={styles.box}>{children}</div>;
};
