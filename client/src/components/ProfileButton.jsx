import { useState } from "react";
import Button from "./Button";
import styles from "./ProfileButton.module.css";

const ProfileButton = ({ options }) => {
    const [isMenu, setIsMenu] = useState(false);
    return (
        <div className={styles.profileContainer}>
            <div className={styles.profileCircle} onClick={() => setIsMenu((prev) => !prev)}>
                🐨
            </div>
            <div className={`${styles.profileMenu} ${isMenu ? styles.show : ""}`}>
                {options &&
                    options.map((item, idx) => {
                        return (
                            <Button
                                key={"profile-button-" + idx}
                                text={item[0]}
                                onClick={item[1]}
                            />
                        );
                    })}
            </div>
        </div>
    );
};

export default ProfileButton;
